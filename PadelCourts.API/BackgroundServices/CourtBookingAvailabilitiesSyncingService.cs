using System.Diagnostics;
using Microsoft.Extensions.Options;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace WebApplication1.BackgroundServices;

public class CourtBookingAvailabilitiesSyncingService : BackgroundService
{
    private readonly ILogger<CourtBookingAvailabilitiesSyncingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICourtBookingProviderResolver _courtBookingProviderResolver;
    private readonly CourtBookingAvailabilitiesSyncOptions _options;
    private readonly TimeSpan _courtAvailabilitiesUpdatePeriod;

    public CourtBookingAvailabilitiesSyncingService(
        ILogger<CourtBookingAvailabilitiesSyncingService> logger, 
        IServiceProvider serviceProvider, 
        ICourtBookingProviderResolver courtBookingProviderResolver,
        IOptions<CourtBookingAvailabilitiesSyncOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _courtBookingProviderResolver = courtBookingProviderResolver;
        _options = options.Value;
        _courtAvailabilitiesUpdatePeriod = TimeSpan.FromMinutes(_options.UpdatePeriod);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(_courtAvailabilitiesUpdatePeriod);
        await PerformSyncCycleAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformSyncCycleAsync(stoppingToken);
        }
    }

    private async Task PerformSyncCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var availablePadelClubs = await GetAvailablePadelClubsAsync(stoppingToken, scope);
        await SyncAvailableCourtsAsync(availablePadelClubs, scope, stoppingToken);
    }

    private static async Task<List<PadelClub>> GetAvailablePadelClubsAsync(CancellationToken stoppingToken, IServiceScope scope)
    {
        var padelClubsRepository = scope.ServiceProvider.GetRequiredService<IPadelClubsRepository>();
        var availableClubs = await padelClubsRepository.GetPadelClubs(stoppingToken);
        var clubsList = availableClubs.ToList();
        return clubsList;
    }

    private async Task SyncAvailableCourtsAsync(List<PadelClub> availablePadelClubs, IServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        try
        {
            var startDate = DateTime.Now.Date;
            var endDate = DateTime.Now.Date.AddDays(_options.DaysToSyncCount);

            var syncTasks = availablePadelClubs.Select(async club =>
            {
                var courtBookingProvider = _courtBookingProviderResolver.GetProvider(club.Provider);
                    
                var clubCourtBookingAvailabilitiesSyncResult = await courtBookingProvider.GetCourtBookingAvailabilitiesAsync(club, startDate, endDate, cancellationToken);

                if (clubCourtBookingAvailabilitiesSyncResult.FailedDailyCourtBookingAvailabilitiesSyncResults.Any())
                {
                    foreach (var failedDailyCourtBookingAvailabilitiesSyncResult in clubCourtBookingAvailabilitiesSyncResult.FailedDailyCourtBookingAvailabilitiesSyncResults)
                    {
                        _logger.LogError(failedDailyCourtBookingAvailabilitiesSyncResult.Exception, "{FailureReason} for {ClubName} at {Date}", failedDailyCourtBookingAvailabilitiesSyncResult.Reason, club.Name, failedDailyCourtBookingAvailabilitiesSyncResult.Date);
                    }
                }
                    
                var mostCurrentClubAvailabilities = clubCourtBookingAvailabilitiesSyncResult.CourtAvailabilities;

                if (mostCurrentClubAvailabilities.Any())
                {
                    _logger.LogInformation("Successfully synced {count} availabilities for club: {clubName}", mostCurrentClubAvailabilities.Count, club.Name);   
                }
                    
                return mostCurrentClubAvailabilities;
            }).ToArray();
            
            var availabilitiesPerProvider = await Task.WhenAll(syncTasks);
            var allProvidersCombinedAvailabilities = new List<CourtAvailability>();

            foreach (var providerAvailabilities in availabilitiesPerProvider)
            {
                allProvidersCombinedAvailabilities.AddRange(providerAvailabilities);
            }

            try
            {
                var courtAvailabilityRepository = scope.ServiceProvider.GetRequiredService<ICourtAvailabilityRepository>();
                var existingAvailabilities = await courtAvailabilityRepository.GetAvailabilitiesAsync(startDate.AddDays(-2), endDate.AddDays(2), cancellationToken: cancellationToken);
                await SyncExistingAndMostCurrentAvailabilitiesAsync(allProvidersCombinedAvailabilities, existingAvailabilities, courtAvailabilityRepository, cancellationToken);
                _logger.LogInformation("Finished syncing court booking availabilities from {startDate} to {endDate}", startDate, endDate);
                _logger.LogInformation("Sync completed in {elapsedMilliseconds} ms for {availableClubsCount} clubs", stopWatch.ElapsedMilliseconds, availablePadelClubs.Count);   
            } catch (Exception e)
            {
                _logger.LogError(e, "Error while syncing court booking availabilities at {CurrentDate}", DateTime.Now);
            }
        }
        finally
        {
            stopWatch.Stop();
        }
    }

    private async Task SyncExistingAndMostCurrentAvailabilitiesAsync(IEnumerable<CourtAvailability> mostCurrentAvailabilities, IEnumerable<CourtAvailability> existingAvailabilities, ICourtAvailabilityRepository repository, CancellationToken cancellationToken)
    {
        var mostCurrentAvailabilitiesDictionary = mostCurrentAvailabilities.ToDictionary(c => $"{c.ClubId}_${c.StartTime:o}_${c.EndTime:o}_${c.CourtName}");
        var existingAvailabilitiesDictionary = existingAvailabilities.ToDictionary(c => $"{c.ClubId}_${c.StartTime:o}_${c.EndTime:o}_${c.CourtName}");
        var toAdd = mostCurrentAvailabilitiesDictionary.Keys.Except(existingAvailabilitiesDictionary.Keys).Select(k => mostCurrentAvailabilitiesDictionary[k]).ToList();
        var toRemove = existingAvailabilitiesDictionary.Keys.Except(mostCurrentAvailabilitiesDictionary.Keys).Select(k => existingAvailabilitiesDictionary[k]).ToList();
        await repository.SaveAvailabilitiesAsync(toAdd, cancellationToken);
        await repository.DeleteAvailabilitiesAsync(toRemove, cancellationToken);
    }
}

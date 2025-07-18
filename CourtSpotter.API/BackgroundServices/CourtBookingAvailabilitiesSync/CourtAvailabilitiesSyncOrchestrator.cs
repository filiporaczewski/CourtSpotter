using System.Diagnostics;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Options;
using CourtSpotter.Core.Results;
using Microsoft.Extensions.Options;

namespace CourtSpotter.BackgroundServices.CourtBookingAvailabilitiesSync;

public class CourtAvailabilitiesSyncOrchestrator : ICourtAvailabilitiesSyncOrchestrator
{
    private readonly ILogger<CourtAvailabilitiesSyncOrchestrator> _logger;
    private readonly IPadelClubsRepository _padelClubsRepository;
    private readonly ICourtAvailabilityRepository _courtAvailabilityRepository;
    private readonly ICourtBookingProviderResolver _courtBookingProviderResolver;
    private readonly CourtBookingAvailabilitiesSyncOptions _options;
    private readonly TimeProvider _timeProvider;
    
    public CourtAvailabilitiesSyncOrchestrator(
        ILogger<CourtAvailabilitiesSyncOrchestrator> logger,
        IPadelClubsRepository padelClubsRepository,
        ICourtAvailabilityRepository courtAvailabilityRepository,
        ICourtBookingProviderResolver courtBookingProviderResolver,
        IOptions<CourtBookingAvailabilitiesSyncOptions> options,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _padelClubsRepository = padelClubsRepository;
        _courtAvailabilityRepository = courtAvailabilityRepository;
        _courtBookingProviderResolver = courtBookingProviderResolver;
        _options = options.Value;
        _timeProvider = timeProvider;
    }
    
    public async Task OrchestrateSyncAsync(CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
    
        try
        {
            _logger.LogInformation("Starting court availabilities sync");
            var availablePadelClubs = await _padelClubsRepository.GetPadelClubs(cancellationToken);
            await SyncAvailableCourtsAsync(availablePadelClubs.ToList(), cancellationToken);
            _logger.LogInformation("Sync completed in {ElapsedMs} ms", stopWatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync orchestration");
        }
        finally
        {
            stopWatch.Stop();
        }
    }
    
    private async Task SyncAvailableCourtsAsync(List<PadelClub> availablePadelClubs, CancellationToken cancellationToken)
    {
        var currentUtcTime = _timeProvider.GetUtcNow().DateTime;
        var startDate = currentUtcTime.Date;
        var endDate = currentUtcTime.Date.AddDays(_options.DaysToSyncCount);

        var syncTasks = availablePadelClubs.Select(async club =>
        {
            var courtBookingProvider = _courtBookingProviderResolver.GetProvider(club.Provider);
            var clubResult = await courtBookingProvider.GetCourtBookingAvailabilitiesAsync(club, startDate, endDate, cancellationToken);
            LogFailures(clubResult, club);
        
            if (clubResult.CourtAvailabilities.Any())
            {
                _logger.LogInformation("Successfully synced {Count} availabilities for club: {ClubName}", clubResult.CourtAvailabilities.Count, club.Name);
            }

            return clubResult.CourtAvailabilities;
        });

        var availabilitiesPerProvider = await Task.WhenAll(syncTasks);
        var allAvailabilities = availabilitiesPerProvider.SelectMany(a => a).ToList();
        var existingAvailabilities = await _courtAvailabilityRepository.GetAvailabilitiesAsync(startDate.AddDays(-2), endDate.AddDays(2), cancellationToken: cancellationToken);
        await SyncAvailabilitiesAsync(allAvailabilities, existingAvailabilities, cancellationToken);
        _logger.LogInformation("Finished syncing court booking availabilities from {StartDate} to {EndDate}", startDate, endDate);
    }
    
    private void LogFailures(CourtBookingAvailabilitiesSyncResult result, PadelClub club)
    {
        foreach (var failure in result.FailedDailyCourtBookingAvailabilitiesSyncResults)
        {
            _logger.LogError(failure.Exception, "{FailureReason} for {ClubName} at {Date}", failure.Reason, club.Name, failure.Date);
        }
    }
    
    private async Task SyncAvailabilitiesAsync(IEnumerable<CourtAvailability> current, IEnumerable<CourtAvailability> existing, CancellationToken cancellationToken)
    {
        var (toAdd, toRemove) = CalculateDiff(current, existing);
        await _courtAvailabilityRepository.SaveAvailabilitiesAsync(toAdd, cancellationToken);
        await _courtAvailabilityRepository.DeleteAvailabilitiesAsync(toRemove, cancellationToken);
    }

    private static (List<CourtAvailability> ToAdd, List<CourtAvailability> ToRemove) CalculateDiff(IEnumerable<CourtAvailability> current, IEnumerable<CourtAvailability> existing)
    {
        var currentDict = current.ToDictionary(c => $"{c.ClubId}_{c.StartTime:o}_{c.EndTime:o}_{c.CourtName}");
        var existingDict = existing.ToDictionary(c => $"{c.ClubId}_{c.StartTime:o}_{c.EndTime:o}_{c.CourtName}");

        var toAdd = currentDict.Keys.Except(existingDict.Keys).Select(k => currentDict[k]).ToList();
        var toRemove = existingDict.Keys.Except(currentDict.Keys).Select(k => existingDict[k]).ToList();
    
        return (toAdd, toRemove);
    }
}
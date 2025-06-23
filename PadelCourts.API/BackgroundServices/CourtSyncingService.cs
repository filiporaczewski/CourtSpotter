using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace WebApplication1.BackgroundServices;

public class CourtSyncingService : BackgroundService
{
    private readonly ILogger<CourtSyncingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(5);
    private readonly List<Club> _clubs;

    public CourtSyncingService(ILogger<CourtSyncingService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _clubs = GetHardcodedClubs();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Court Syncing Service started at: {time}", DateTimeOffset.Now);
        using var timer = new PeriodicTimer(_period);
        using var scope = _serviceProvider.CreateScope();
        
        var providerResolver = scope.ServiceProvider.GetRequiredService<ICourtProviderResolver>();
        var repository = scope.ServiceProvider.GetRequiredService<ICourtAvailabilityRepository>();
        await SyncAvailableCourts(providerResolver, repository);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncAvailableCourts(providerResolver, repository);
        }
    }

    private async Task SyncAvailableCourts(ICourtProviderResolver providerResolver, ICourtAvailabilityRepository repository)
    {
        try
        {
            var startDate = DateTime.Now.Date;
            var endDate = DateTime.Now.Date.AddDays(1);
            
            _logger.LogInformation("Starting sync for {clubCount} clubs from {startDate} to {endDate}", 
                _clubs.Count, startDate, endDate);
            
            var dbAvailabilities = await repository.GetAvailabilitiesAsync(startDate, endDate);
            var allProviderAvailabilities = new List<CourtAvailability>();
            
            foreach (var club in _clubs)
            {
                var courtProvider = providerResolver.GetProvider(club.Provider);
                var availabilities = await courtProvider.GetCourtAvailabilities(club, DateTime.Now.Date, DateTime.Now.Date.AddDays(1));
                allProviderAvailabilities.AddRange(availabilities);
            }
            
            await CompareAndSync(repository, allProviderAvailabilities, dbAvailabilities, CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while syncing courts");
        }
    }

    private async Task CompareAndSync(ICourtAvailabilityRepository repository,
        IEnumerable<CourtAvailability> providerAvailabilities, IEnumerable<CourtAvailability> dbAvailabilities,
        CancellationToken cancellationToken)
    {
        var providerKeys = providerAvailabilities
            .Select(a => $"{a.ClubId}_{a.StartTime:yyyy-MM-dd HH:mm}_{a.EndTime:yyyy-MM-dd HH:mm}_{a.CourtName}_{a.Provider}")
            .ToHashSet();
        
        var dbKeys = dbAvailabilities
            .Select(a => $"{a.ClubId}_{a.StartTime:yyyy-MM-dd HH:mm}_{a.EndTime:yyyy-MM-dd HH:mm}_{a.CourtName}__{a.Provider}")
            .ToHashSet();

        // Find items to insert (in provider but not in DB)
        var toInsert = providerAvailabilities
            .Where(pa => !dbKeys.Contains($"{pa.ClubId}_{pa.StartTime:yyyy-MM-dd HH:mm}_{pa.EndTime:yyyy-MM-dd HH:mm}_{pa.CourtName}_{pa.Provider}"))
            .ToList();

        // Find items to delete (in DB but not in provider)
        var toDelete = dbAvailabilities
            .Where(da => !providerKeys.Contains($"{da.ClubId}_{da.StartTime:yyyy-MM-dd HH:mm}_{da.EndTime:yyyy-MM-dd HH:mm}_{da.CourtName}_{da.Provider}"))
            .ToList();

        if (toInsert.Any())
        {
            await repository.SaveAvailabilitiesAsync(toInsert, cancellationToken);
        }

        foreach (var toDeleteItem in toDelete)
        {
            await repository.DeleteAvailabilityAsync(toDeleteItem, cancellationToken);
        }
    }

    private List<Club> GetHardcodedClubs()
    {
        return
        [
            new Club
            {
                Id = "1",
                Name = "Interpadel",
                Provider = ProviderType.Playtomic
            },

            new Club
            {
                Id = "2",
                Name = "Padlovnia",
                Provider = ProviderType.KlubyOrg
            },

            new Club
            {
                Id = "3",
                Name = "ProPadel Jutrzenki",
                Provider = ProviderType.CourtMe
            }
        ];
    }

}
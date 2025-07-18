using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Options;
using Microsoft.Extensions.Options;

namespace CourtSpotter.BackgroundServices.CourtBookingAvailabilitiesSync;

public class CourtBookingAvailabilitiesSyncingService : BackgroundService
{
    private readonly ILogger<CourtBookingAvailabilitiesSyncingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _updatePeriod;

    public CourtBookingAvailabilitiesSyncingService(
        ILogger<CourtBookingAvailabilitiesSyncingService> logger,
        IServiceProvider serviceProvider,
        IOptions<CourtBookingAvailabilitiesSyncOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _updatePeriod = TimeSpan.FromMinutes(options.Value.UpdatePeriod);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(_updatePeriod);
        await PerformSyncCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformSyncCycleAsync(stoppingToken);
        }
    }

    private async Task PerformSyncCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var courtBookingAvailabilitiesSyncOrchestrator = scope.ServiceProvider.GetRequiredService<ICourtAvailabilitiesSyncOrchestrator>();
            await courtBookingAvailabilitiesSyncOrchestrator.OrchestrateSyncAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync cycle");
        }
    }
}
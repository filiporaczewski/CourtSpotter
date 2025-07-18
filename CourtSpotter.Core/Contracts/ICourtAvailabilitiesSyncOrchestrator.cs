namespace CourtSpotter.Core.Contracts;

public interface ICourtAvailabilitiesSyncOrchestrator
{
    public Task OrchestrateSyncAsync(CancellationToken cancellationToken = default);
}
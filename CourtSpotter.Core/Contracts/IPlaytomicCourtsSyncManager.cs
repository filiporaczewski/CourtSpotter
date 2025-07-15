using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface IPlaytomicCourtsSyncManager
{
    Task<IEnumerable<PlaytomicCourt>> RetrievePlaytomicCourts(string clubName, CancellationToken cancellationToken = default);
}
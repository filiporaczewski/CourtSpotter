using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface IPlaytomicCourtsSyncManager
{
    Task<IEnumerable<PlaytomicCourt>> RetrievePlaytomicCourts(PadelClub club, CancellationToken cancellationToken = default);
}
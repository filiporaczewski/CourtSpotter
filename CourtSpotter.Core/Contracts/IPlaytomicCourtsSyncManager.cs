using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface IPlaytomicCourtsSyncManager
{
    Task<IEnumerable<PlaytomicCourt>> RetrievePlaytomicCourts(PadelClub club, CancellationToken cancellationToken = default);
}
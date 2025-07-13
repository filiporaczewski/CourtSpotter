using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface IPlaytomicCourtsRepository
{
    Task<IEnumerable<PlaytomicCourt>> GetPlaytomicCourts(CancellationToken cancellationToken = default);
    
    Task<IEnumerable<PlaytomicCourt>> GetPlaytomicCourtsByClubId(string clubId, CancellationToken cancellationToken = default);
    
    Task AddPlaytomicCourt(PlaytomicCourt court, CancellationToken cancellationToken = default);

    Task AddPlaytomicCourts(IEnumerable<PlaytomicCourt> courts, CancellationToken cancellationToken = default);
}
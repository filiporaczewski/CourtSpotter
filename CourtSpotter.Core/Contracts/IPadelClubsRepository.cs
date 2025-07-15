using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface IPadelClubsRepository
{
    Task<IEnumerable<PadelClub>> GetPadelClubs(CancellationToken cancellationToken = default);

    Task<PadelClub?> GetByName(string name, CancellationToken cancellationToken = default);

    Task AddPadelClub(PadelClub newClub, ProviderType provider, CancellationToken cancellationToken = default);
}
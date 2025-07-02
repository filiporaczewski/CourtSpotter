using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface IPadelClubsRepository
{
    Task<IEnumerable<PadelClub>> GetPadelClubs(CancellationToken cancellationToken = default);

    Task<PadelClub?> GetByName(string name, CancellationToken cancellationToken = default);

    Task AddPadelClub(string name, ProviderType provider, CancellationToken cancellationToken = default);
}
using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface ICourtProvider
{
    Task<IEnumerable<CourtAvailability>> GetCourtAvailabilities(
        Club club,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );
}
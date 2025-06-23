using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface ICourtAvailabilityRepository
{
    Task SaveAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default);
    Task<IEnumerable<CourtAvailability>> GetAvailabilitiesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task DeleteAvailabilityAsync(CourtAvailability availability, CancellationToken cancellationToken = default);
    Task DeleteOldAvailabilitiesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
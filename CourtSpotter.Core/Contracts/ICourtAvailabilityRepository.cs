using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface ICourtAvailabilityRepository
{
    Task SaveAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default);
    Task<IEnumerable<CourtAvailability>> GetAvailabilitiesAsync(DateTime startDate, DateTime endDate, int[]? durationFilters = null, string[]? clubIds = null, CourtType? courtType = null, CancellationToken cancellationToken = default);
    Task DeleteAvailabilityAsync(CourtAvailability availability, CancellationToken cancellationToken = default);
    
    Task DeleteAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default);
}
using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface ICourtBookingProvider
{
    Task<IEnumerable<CourtAvailability>> GetCourtBookingAvailabilitiesAsync(
        PadelClub padelClub,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );
}
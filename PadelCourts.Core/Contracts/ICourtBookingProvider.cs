using PadelCourts.Core.Models;
using PadelCourts.Core.Results;

namespace PadelCourts.Core.Contracts;

public interface ICourtBookingProvider
{
    Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(
        PadelClub padelClub,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );
}
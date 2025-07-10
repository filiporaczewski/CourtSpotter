using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Core.Results;

namespace PadelCourts.Infrastructure.BookingProviders;

public class CourtBookingMeProvider : ICourtBookingProvider
{
    public async Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(
        PadelClub padelClub, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        return new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = [],
            FailedDailyCourtBookingAvailabilitiesSyncResults = []
        };
    }
}
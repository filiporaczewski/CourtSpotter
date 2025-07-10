using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Results;

namespace CourtSpotter.Infrastructure.BookingProviders;

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
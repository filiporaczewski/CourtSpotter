using CourtSpotter.Core.Models;
using CourtSpotter.Core.Results;

namespace CourtSpotter.Core.Contracts;

public interface ICourtBookingProvider
{
    Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(
        PadelClub padelClub,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );
}
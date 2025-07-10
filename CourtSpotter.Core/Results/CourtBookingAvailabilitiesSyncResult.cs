using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Results;

public class CourtBookingAvailabilitiesSyncResult
{
    public List<CourtAvailability> CourtAvailabilities { get; init; } = null!;

    public List<FailedDailyCourtBookingAvailabilitiesSyncResult> FailedDailyCourtBookingAvailabilitiesSyncResults
    {
        get;
        init;
    } = null!;
}

public class FailedDailyCourtBookingAvailabilitiesSyncResult
{
    public DateOnly Date { get; init; }
    public string Reason { get; init; } = null!;
    
    public Exception? Exception { get; init; }
}

public class DailyCourtBookingAvailabilitiesSyncResult
{
    public DateOnly Date { get; init; }
    public List<CourtAvailability> CourtAvailabilities { get; init; } = null!;
    public bool Success { get; init; }
    public string? FailureReason{ get; init; }
    
    public Exception? Exception { get; init; }

    public static DailyCourtBookingAvailabilitiesSyncResult CreateFailedResult(DateOnly date, string reason, Exception? exception = null)

    {
        return new DailyCourtBookingAvailabilitiesSyncResult
        {
            Date = date,
            CourtAvailabilities = [],
            Success = false,
            FailureReason = reason,
            Exception = exception
        };
    }
}
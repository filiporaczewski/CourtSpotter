namespace CourtSpotter.Core.Options;

public class CourtBookingAvailabilitiesSyncOptions
{
    public int DaysToSyncCount { get; set; } = 14;
    public int UpdatePeriod { get; set; } = 10; // in minutes

    public int EarliestBookingHour { get; set; } = 6;
    
    public int LatestBookingHour { get; set; } = 22;
}
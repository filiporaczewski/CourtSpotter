namespace WebApplication1.BackgroundServices;

public class CourtBookingAvailabilitiesSyncOptions
{
    public int DaysToSyncCount { get; set; } = 14;
    public int UpdatePeriod { get; set; } = 10; // in minutes
}
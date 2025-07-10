namespace CourtSpotter.Infrastructure.BookingProviders.Playtomic;

public class PlaytomicProviderOptions
{
    public string ApiTimeZoneId { get; set; }
    
    public string ApiBaseUrl { get; set; }
    
    public string LocalTimeZoneId { get; set; }

    public int EarliestBookingHour { get; set; }

    public int LatestBookingHour { get; set; }
}
namespace CourtSpotter.Infrastructure.BookingProviders.RezerwujKort;

public class DailyCourtBookingAvailabilitiesEndpointApiResponse
{
    public List<Court> Courts { get; set; } = [];
    public string Date { get; set; } = string.Empty;
}

public class Court
{
    public int CourtId { get; set; }
    public string CourtName { get; set; }
    public string CourtNameWww { get; set; } = string.Empty;
    
    public string CourtDescription { get; set; } = string.Empty;
    public bool OnlineReservation { get; set; }
    public List<Hour> Hours { get; set; } = [];
}

public class Hour
{
    public int HourId { get; set; }
    public string HourName { get; set; } = string.Empty;
    public string HourStatus { get; set; } = string.Empty;
    public List<int> PossibleHalfHourSlots { get; set; } = [];
}
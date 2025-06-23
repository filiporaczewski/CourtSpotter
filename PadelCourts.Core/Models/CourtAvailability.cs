namespace PadelCourts.Core.Models;

public class CourtAvailability
{
    public string Id { get; set; }
    
    public string ClubId { get; set; }

    public string ClubName { get; set; }

    public string CourtName { get; set; }
    
    public CourtType Type { get; set; } = CourtType.Indoor;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "PLN";
    
    public TimeSpan Duration => EndTime - StartTime;

    public string? BookingUrl { get; set; }
    
    public ProviderType Provider { get; set; }
}
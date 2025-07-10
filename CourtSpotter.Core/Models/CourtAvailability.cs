using System.Text.Json.Serialization;

namespace CourtSpotter.Core.Models;

public class CourtAvailability
{
    public string Id { get; init; }
    
    public string ClubId { get; init; }

    public string ClubName { get; init; }

    public string CourtName { get; init; }
    
    [JsonPropertyName("type")]
    public CourtType Type { get; init; } = CourtType.Indoor;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = "PLN";
    
    public TimeSpan Duration => EndTime - StartTime;

    public string? BookingUrl { get; init; }
    
    [JsonPropertyName("provider")]
    public ProviderType Provider { get; init; }
}
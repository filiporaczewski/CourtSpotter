using System.Text.Json.Serialization;

namespace PadelCourts.Infrastructure.BookingProviders;

public class TenantAvailability
{
    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; }
    
    [JsonPropertyName("start_date")]
    public string StartDate { get; set; }
    
    [JsonPropertyName("slots")]
    public List<TimeSlot> Slots { get; set; }
}

public class TimeSlot
{
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    
    [JsonPropertyName("price")]
    public string Price { get; set; }
}


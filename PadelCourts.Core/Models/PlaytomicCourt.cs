using System.Text.Json.Serialization;

namespace PadelCourts.Core.Models;

public class PlaytomicCourt
{
    public string Id { get; set; }
    
    public string ClubId { get; set; }
    
    public string Name { get; set; }
    
    [JsonPropertyName("courtType")]
    public CourtType Type { get; set; }
}
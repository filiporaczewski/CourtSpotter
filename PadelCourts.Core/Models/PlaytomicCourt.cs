using System.Text.Json.Serialization;

namespace PadelCourts.Core.Models;

public class PlaytomicCourt
{
    public string Id { get; init; }
    
    public string ClubId { get; init; }
    
    public string Name { get; init; }
    
    [JsonPropertyName("courtType")]
    public CourtType Type { get; init; }
}
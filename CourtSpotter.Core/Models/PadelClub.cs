using System.Text.Json.Serialization;

namespace PadelCourts.Core.Models;

public class PadelClub
{
    public string Id => ClubId;
    
    public string ClubId { get; init; }
    public string Name { get; init; }
    
    [JsonPropertyName("provider")]
    public ProviderType Provider { get; init; }
    
    public int? PagesCount { get; init; }
}
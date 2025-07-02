using System.Text.Json.Serialization;

namespace PadelCourts.Core.Models;

public class PadelClub
{
    public string Id => ClubId;
    
    public string ClubId { get; set; }
    public string Name { get; set; }
    
    [JsonPropertyName("provider")]
    public ProviderType Provider { get; set; }
    
    public int? PagesCount { get; set; }
}
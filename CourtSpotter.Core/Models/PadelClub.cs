using System.Text.Json.Serialization;

namespace CourtSpotter.Core.Models;

public class PadelClub
{
    public static PadelClub Create(string clubId, string name, ProviderType provider, string timeZone = "Europe/Warsaw", int? pagesCount = null)
    {
        return new PadelClub
        {
            ClubId = clubId,
            Name = name,
            Provider = provider,
            PagesCount = pagesCount,
            TimeZone = timeZone
        };
    }
    
    public string Id => ClubId;
    
    public string ClubId { get; init; }
    public string Name { get; init; }
    
    [JsonPropertyName("provider")]
    public ProviderType Provider { get; init; }
    
    public int? PagesCount { get; init; }

    public string TimeZone { get; set; } = "Europe/Warsaw";
}
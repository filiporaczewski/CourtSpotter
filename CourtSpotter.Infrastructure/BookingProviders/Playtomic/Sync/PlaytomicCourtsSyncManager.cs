using System.Text.Json;
using AngleSharp;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;

namespace CourtSpotter.Infrastructure.BookingProviders.Playtomic.Sync;

public class PlaytomicCourtsSyncManager : IPlaytomicCourtsSyncManager
{
    private readonly HttpClient _httpClient;

    public PlaytomicCourtsSyncManager(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("PlaytomicClient");
    }

    public async Task<IEnumerable<PlaytomicCourt>> RetrievePlaytomicCourts(PadelClub club, CancellationToken cancellationToken = default)
    {
        var playtomicUrl = $"https://playtomic.com/clubs/{GetUrlSuffix(club.Name)}";
        var html = await _httpClient.GetStringAsync(playtomicUrl, cancellationToken);
        
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), cancel: cancellationToken);
        
        var nextDataScript = document.QuerySelector("script#__NEXT_DATA__");
        
        if (nextDataScript?.TextContent is null)
        {
            return new List<PlaytomicCourt>();
        }

        try
        {
            var jsonData = JsonSerializer.Deserialize<PlaytomicCourtsJsonResponse>(nextDataScript.TextContent);

            if (jsonData is null)
            {
                return new List<PlaytomicCourt>();           
            }
            
            var filteredCourts = jsonData.Props.PageProps.Tenant.Resources
                .Where(r => r.Properties.ResourceSize != "single")
                .ToList();

            return filteredCourts.Select(item => new PlaytomicCourt
            {
                Name = item.Name,
                Id = item.ResourceId,
                Type = GetCourtType(item.Properties.ResourceType),
                ClubId = club.ClubId
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error parsing Playtomic JSON: {e.Message}");
            return new List<PlaytomicCourt>();
        }
    }
    
    private string GetUrlSuffix(string clubName)
    {
        return clubName.ToLowerInvariant().Replace(" ", "-");
    }

    private CourtType GetCourtType(string courtType)
    {
        return courtType switch
        {
            "indoor" => CourtType.Indoor,
            "outdoor" => CourtType.Outdoor,
            _ => CourtType.Indoor
        };
    }
}
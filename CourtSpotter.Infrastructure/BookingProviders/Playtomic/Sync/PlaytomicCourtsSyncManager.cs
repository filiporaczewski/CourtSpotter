using System.Text.Json;
using AngleSharp;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CourtSpotter.Infrastructure.BookingProviders.Playtomic.Sync;

public class PlaytomicCourtsSyncManager : IPlaytomicCourtsSyncManager
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlaytomicCourtsSyncManager> _logger;

    public PlaytomicCourtsSyncManager(IHttpClientFactory httpClientFactory, ILogger<PlaytomicCourtsSyncManager> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PlaytomicClient");
        _logger = logger;
    }

    public async Task<IEnumerable<PlaytomicCourt>> RetrievePlaytomicCourts(string clubName, CancellationToken cancellationToken = default)
    {
        var playtomicUrl = $"https://playtomic.com/clubs/{GetUrlSuffix(clubName)}";
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

            var tenant = jsonData.Props.PageProps.Tenant;

            var filteredCourts = tenant.Resources
                .Where(r => r.Properties.ResourceSize != "single")
                .ToList();

            return filteredCourts.Select(item => new PlaytomicCourt
            {
                Name = item.Name,
                Id = item.ResourceId,
                Type = GetCourtType(item.Properties.ResourceType),
                ClubId = tenant.TenantId
            });
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Network error calling Playtomic API (Court sync)");
            return new List<PlaytomicCourt>();
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError(e, "Timeout when calling Playtomic API (Court sync)");
            return new List<PlaytomicCourt>();
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Invalid JSON response from Playtomic API (Court sync)");
            return new List<PlaytomicCourt>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error when syncing playtomic courts for club {clubName}", clubName);
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
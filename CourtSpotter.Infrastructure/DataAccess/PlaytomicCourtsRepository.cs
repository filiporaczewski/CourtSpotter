using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Microsoft.Azure.Cosmos;

namespace CourtSpotter.Infrastructure.DataAccess;

public class PlaytomicCourtsRepository : IPlaytomicCourtsRepository
{
    private readonly Container _container;

    public PlaytomicCourtsRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer("PadelAvailabilitiesDb", "PlaytomicCourtsV2");
    }
    
    public async Task<IEnumerable<PlaytomicCourt>> GetPlaytomicCourts(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");
        
        var results = new List<PlaytomicCourt>();
        using var iterator = _container.GetItemQueryIterator<PlaytomicCourt>(query);
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IEnumerable<PlaytomicCourt>> GetPlaytomicCourtsByClubId(string clubId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.clubId = @clubId")
            .WithParameter("@clubId", clubId);
        
        var results = new List<PlaytomicCourt>();
        using var iterator = _container.GetItemQueryIterator<PlaytomicCourt>(query);
        
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task AddPlaytomicCourt(PlaytomicCourt court, CancellationToken cancellationToken = default)
    {
        await _container.CreateItemAsync(court, new PartitionKey(court.Name), cancellationToken: cancellationToken);;
    }
}
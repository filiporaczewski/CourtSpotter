using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Microsoft.Azure.Cosmos;

namespace CourtSpotter.Infrastructure.DataAccess;

public class PadelClubsRepository : IPadelClubsRepository
{
    private readonly Container _container;

    public PadelClubsRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer("PadelAvailabilitiesDb", "PadelClubs");
    }
    
    public async Task<IEnumerable<PadelClub>> GetPadelClubs(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");
        
        var results = new List<PadelClub>();
        using var iterator = _container.GetItemQueryIterator<PadelClub>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
        
        return results;
    }

    public async Task<PadelClub?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.name = @clubName").WithParameter("@clubName", name);
        
        using var iterator = _container.GetItemQueryIterator<PadelClub>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            var club = response.FirstOrDefault();
            if (club != null)
            {
                return club;
            }
        }
    
        return null;
    }

    public async Task AddPadelClub(string name, ProviderType provider, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString();
        
        var newClub = new PadelClub
        {
            Name = name,
            Provider = provider,
            ClubId = id
        };
        
        await _container.CreateItemAsync(newClub, new PartitionKey(id), cancellationToken: cancellationToken);
    }
}
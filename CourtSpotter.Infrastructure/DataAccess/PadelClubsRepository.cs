using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace CourtSpotter.Infrastructure.DataAccess;

public class PadelClubsRepository : IPadelClubsRepository
{
    private readonly Container _container;

    public PadelClubsRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var containerId = configuration.GetValue<string>("CosmosDbContainers:PadelClubs");
        _container = cosmosClient.GetContainer(databaseId: "PadelAvailabilitiesDb", containerId);
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

    public async Task AddPadelClub(PadelClub newClub, ProviderType provider, CancellationToken cancellationToken = default)
    {
        await _container.CreateItemAsync(newClub, new PartitionKey(newClub.ClubId), cancellationToken: cancellationToken);
    }
}
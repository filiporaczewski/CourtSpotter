using Microsoft.Azure.Cosmos;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.DataAccess;

public class CourtAvailabilityRepository : ICourtAvailabilityRepository
{
    private readonly Container _container;
    
    public CourtAvailabilityRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer("PadelCourtBookingDb", "PadelCourtAvailabilities");
    }
    
    public async Task SaveAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default)
    {
        foreach (var availability in availabilities)
        {
            await _container.UpsertItemAsync(availability, new PartitionKey(availability.ClubId), cancellationToken: cancellationToken);
        }
    }

    public async Task<IEnumerable<CourtAvailability>> GetAvailabilitiesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.startTime >= @startDate AND c.endTime <= @endDate")
            .WithParameter("@startDate", startDate.Date)
            .WithParameter("@endDate", endDate.Date);
        
        var results = new List<CourtAvailability>();
        using var iterator = _container.GetItemQueryIterator<CourtAvailability>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
        
        return results;
    }

    public async Task DeleteAvailabilityAsync(CourtAvailability availability, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<CourtAvailability>(availability.Id, new PartitionKey(availability.ClubId), cancellationToken: cancellationToken);            
        } 
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Item already deleted, continue
        }

    }
    
    public async Task DeleteOldAvailabilitiesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT c.id, c.ClubId FROM c WHERE c.Date < @olderThan").WithParameter("@olderThan", olderThan.Date);
        var itemsToDelete = new List<(string id, string clubId)>();
        using var iterator = _container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                itemsToDelete.Add((item.id.ToString(), item.ClubId.ToString()));
            }
        }
        
        foreach (var (id, clubId) in itemsToDelete)
        {
            try
            {
                await _container.DeleteItemAsync<CourtAvailability>(id, new PartitionKey(clubId), cancellationToken: cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
        }
    }
}
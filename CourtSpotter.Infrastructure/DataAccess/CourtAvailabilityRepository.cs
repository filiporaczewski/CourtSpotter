using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Utils;
using Microsoft.Azure.Cosmos;

namespace CourtSpotter.Infrastructure.DataAccess;

public class CourtAvailabilityRepository : ICourtAvailabilityRepository
{
    private readonly Container _container;
    
    public CourtAvailabilityRepository(CosmosClient cosmosClient)
    {
        _container = cosmosClient.GetContainer("PadelAvailabilitiesDb", "CourtAvailabilities");
    }
    
    public async Task SaveAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default)
    {
        var batches = CollectionUtils.CreateBatches(availabilities, 100).ToList();
        
        foreach (var batch in batches)
        {
            await Task.WhenAll(batch.Select(availability => _container.UpsertItemAsync(availability, new PartitionKey(availability.Id), cancellationToken: cancellationToken)));
            await Task.Delay(500, cancellationToken);
        }
    }

    public async Task<IEnumerable<CourtAvailability>> GetAvailabilitiesAsync(DateTime startDate, DateTime endDate, int[]? durationFilters = null, string[]? clubIds = null, CourtType? courtType = null, CancellationToken cancellationToken = default)
    {
        var queryText = "SELECT * FROM c WHERE c.startTime >= @startDate AND c.endTime <= @endDate";
        
        var parameters = new Dictionary<string, object>
        {
            ["@startDate"] = startDate,
            ["@endDate"] = endDate
        };

        if (durationFilters != null && durationFilters.Length > 0)
        {
            var durationTimeSpans = durationFilters.Select(minutes => 
                TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm\:ss")).ToArray();

            var durationConditions = new List<string>();
            for (int i = 0; i < durationTimeSpans.Length; i++)
            {
                var paramName = $"@duration{i}";
                durationConditions.Add($"c.duration = {paramName}");
                parameters[paramName] = durationTimeSpans[i];
            }
            
            queryText += $" AND ({string.Join(" OR ", durationConditions)})";
        }

        if (clubIds != null && clubIds.Length > 0)
        {
            var clubIdsConditions = new List<string>();
            for (int i = 0; i < clubIds.Length; i++)
            {
                var paramName = $"@clubId{i}";
                clubIdsConditions.Add($"c.clubId = {paramName}");
                parameters[paramName] = clubIds[i];
            }
            
            queryText += $" AND ({string.Join(" OR ", clubIdsConditions)})";       
        }

        if (courtType != null)
        {
            queryText += $" AND c.type = {(int)courtType}";
            parameters.Add("@courtType", courtType);
        }
        
        var query = new QueryDefinition(queryText);
        foreach (var param in parameters)
        {
            query = query.WithParameter(param.Key, param.Value);
        }
        
        var results = new List<CourtAvailability>();
        using var iterator = _container.GetItemQueryIterator<CourtAvailability>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
        
        return results;
    }

    public async Task DeleteAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default)
    {
        var batches = CollectionUtils.CreateBatches(availabilities, 100).ToList();
        
        foreach (var batch in batches)
        {
            await Task.WhenAll(batch.Select(availability => DeleteAvailabilityAsync(availability, cancellationToken)));
            // azure limit 1000RU/s
            await Task.Delay(500, cancellationToken);
        }
    }

    public async Task DeleteAvailabilityAsync(CourtAvailability availability, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<CourtAvailability>(availability.Id, new PartitionKey(availability.Id), cancellationToken: cancellationToken);            
        } 
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Item already deleted, continue
        }
    }
}
using System.Net;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CourtSpotter.Infrastructure.DataAccess;

public class CourtAvailabilityRepository : ICourtAvailabilityRepository
{
    private readonly Container _container;
    private readonly ILogger<CourtAvailabilityRepository> _logger;
    
    public CourtAvailabilityRepository(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CourtAvailabilityRepository> logger)
    {
        _logger = logger;
        var containerId = configuration.GetValue<string>("CosmosDbContainers:CourtAvailabilities");
        _container = cosmosClient.GetContainer(databaseId: "PadelAvailabilitiesDb", containerId);
    }
    
    public async Task SaveAvailabilitiesAsync(IEnumerable<CourtAvailability> availabilities, CancellationToken cancellationToken = default)
    {
        var batches = CollectionUtils.CreateBatches(availabilities, 100).ToList();
        
        foreach (var batch in batches)
        {
            await Task.WhenAll(batch.Select(availability => _container.CreateItemAsync(availability, new PartitionKey(availability.Id), cancellationToken: cancellationToken)));
            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task SaveAvailabilityAsync(CourtAvailability availability, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.CreateItemAsync(availability, new PartitionKey(availability.Id), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            _logger.LogWarning("Availability item too large, skipping: {AvailabilityId}", availability.Id);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited during adding availability, retrying: {AvailabilityId}", availability.Id);
            await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(1), cancellationToken);
            await SaveAvailabilityAsync(availability, cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to add availability: {AvailabilityId}, Status: {StatusCode}", availability.Id, ex.StatusCode);
            throw;
        }
    }

    public async Task<IEnumerable<CourtAvailability>> GetAvailabilitiesAsync(DateTime startDate, DateTime endDate, int[]? durationFilters = null, string[]? clubIds = null, CourtType? courtType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryText = GetCourtAvailabilitiesQueryTextBuilder.BuildQueryText(startDate, endDate, durationFilters, clubIds, courtType, out var parameters);
            var query = new QueryDefinition(queryText);

            query = parameters.Aggregate(query, (current, param) => current.WithParameter(param.Key, param.Value));

            var results = new List<CourtAvailability>();
            using var iterator = _container.GetItemQueryIterator<CourtAvailability>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }
        
            return results;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError(ex, "Invalid query parameters for GetAvailabilities");
            throw new ArgumentException("Invalid query parameters", ex);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited during query");
            throw;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to query availabilities, Status: {StatusCode}", ex.StatusCode);
            throw;
        }
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
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Availability already deleted: {AvailabilityId}", availability.Id);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited during delete: {AvailabilityId}", availability.Id);
            await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(1), cancellationToken);
            await DeleteAvailabilityAsync(availability, cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to delete availability: {AvailabilityId}, Status: {StatusCode}", availability.Id, ex.StatusCode);
            throw;
        }
    }
}
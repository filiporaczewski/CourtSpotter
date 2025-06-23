using Microsoft.Azure.Cosmos;
using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.DataAccess;

namespace WebApplication1.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<CosmosClient>(_ =>
        {
            var connectionString = configuration.GetConnectionString("CosmosDb");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("CosmosDb connection string is not configured.");
            }

            return new CosmosClient(connectionString, new CosmosClientOptions()
            {
                ApplicationName = "PadelCourtBookingApp",
                ConnectionMode = ConnectionMode.Direct,
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });

        });
        
        services.AddScoped<ICourtAvailabilityRepository, CourtAvailabilityRepository>();
        
        return services;
    }
}
using Microsoft.Azure.Cosmos;
using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.BookingProviders.Playtomic.Sync;
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

            return new CosmosClient(connectionString, new CosmosClientOptions
            {
                ApplicationName = "PadelCourtBookingApp",
                ConnectionMode = ConnectionMode.Gateway,
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                },
                RequestTimeout = TimeSpan.FromSeconds(60),
                AllowBulkExecution = true
            });

        });
        
        services.AddScoped<ICourtAvailabilityRepository, CourtAvailabilityRepository>();
        services.AddScoped<IPadelClubsRepository, PadelClubsRepository>();
        services.AddScoped<IPlaytomicCourtsRepository, PlaytomicCourtsRepository>();
        services.AddScoped<IPlaytomicCourtsSyncManager, PlaytomicCourtsSyncManager>();
        
        return services;
    }
}
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Cosmos;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.BookingProviders.Playtomic.Sync;
using PadelCourts.Infrastructure.DataAccess;

namespace CourtSpotter.Extensions;

public static class ServiceCollectionInfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, ILoggingBuilder logging, IWebHostEnvironment environment)
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
        
        logging.ClearProviders();

        var azureMonitorConnectionString = configuration["AzureMonitor:ConnectionString"];
        
        logging.AddOpenTelemetry(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("api-padelcourtsearch"));

            if (environment.IsDevelopment())
            {
                b.AddConsoleExporter();
            } else if (environment.IsProduction() && !string.IsNullOrEmpty(azureMonitorConnectionString))
            {
                b.AddAzureMonitorLogExporter(options =>
                {
                    options.ConnectionString = azureMonitorConnectionString;
                });
            }
        });
        
        services.AddOpenTelemetry()
            .WithTracing(b =>
            {
                b.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("api-padelcourtsearch")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = environment.EnvironmentName
                        })
                    )
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = (httpContext) =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLower();

                            if (environment.IsProduction())
                            {
                                return !path?.Contains("/health") == true;
                            }

                            return true;
                        };
                    })
                    .AddHttpClientInstrumentation();

                if (environment.IsDevelopment())
                {
                    b.AddConsoleExporter();
                } else if(environment.IsProduction() && !string.IsNullOrEmpty(azureMonitorConnectionString))
                {
                    b.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = azureMonitorConnectionString;
                    });
                }
            })
            .WithMetrics(m =>
            {
                m.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("api-padelcourtsearch"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (environment.IsProduction() && !string.IsNullOrEmpty(azureMonitorConnectionString))
                {
                    m.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = azureMonitorConnectionString;
                    });
                }
            });
        
        return services;
    }
}
using System.Net;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Infrastructure.BookingProviders;
using CourtSpotter.Infrastructure.BookingProviders.Playtomic;
using CourtSpotter.Infrastructure.BookingProviders.RezerwujKort;
using CourtSpotter.Resolvers;

namespace CourtSpotter.Extensions;

public static class ServiceCollectionBookingProvidersExtensions
{
    public static IServiceCollection AddBookingProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<PlaytomicBookingProvider>();
        services.AddSingleton<KlubyOrgCourtBookingProvider>();
        services.AddSingleton<CourtBookingMeProvider>();
        services.AddSingleton<RezerwujKortBookingProvider>();
        services.AddSingleton<CaseInsensitiveJsonSerializerOptions>(_ => new CaseInsensitiveJsonSerializerOptions());
        services.AddSingleton<ICourtBookingProviderResolver, CourtBookingProviderResolver>();
        
        services.AddHttpClient("PlaytomicClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("RezerwujKortClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddSingleton<CookieContainer>();

        services.AddSingleton<HttpClientHandler>(sp =>
        {
            var container = sp.GetRequiredService<CookieContainer>();
            return new HttpClientHandler
            {
                CookieContainer = container,
                UseCookies = true
            };
        });

        services.AddHttpClient("KlubyOrgClient", client =>
        {
            client.BaseAddress = new Uri("https://kluby.org/");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpClientHandler>());
        
        return services;
    }
}
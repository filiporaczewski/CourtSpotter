using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Infrastructure.BookingProviders;
using CourtSpotter.Infrastructure.BookingProviders.Playtomic;
using CourtSpotter.Infrastructure.BookingProviders.RezerwujKort;

namespace CourtSpotter.Resolvers;

public class CourtBookingProviderResolver : ICourtBookingProviderResolver
{
    private readonly IServiceProvider _serviceProvider;

    public CourtBookingProviderResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ICourtBookingProvider GetProvider(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.CourtMe => _serviceProvider.GetRequiredService<CourtBookingMeProvider>(),
            ProviderType.KlubyOrg => _serviceProvider.GetRequiredService<KlubyOrgCourtBookingProvider>(),
            ProviderType.Playtomic => _serviceProvider.GetRequiredService<PlaytomicBookingProvider>(),
            ProviderType.RezerwujKort => _serviceProvider.GetRequiredService<RezerwujKortBookingProvider>(),
            _ => throw new NotSupportedException("Provider not supported.")
        };
    }
}
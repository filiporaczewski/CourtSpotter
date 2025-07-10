using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Infrastructure.BookingProviders;
using PadelCourts.Infrastructure.BookingProviders.Playtomic;
using PadelCourts.Infrastructure.BookingProviders.RezerwujKort;

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
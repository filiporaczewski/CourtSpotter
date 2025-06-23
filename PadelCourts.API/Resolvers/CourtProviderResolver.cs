using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Infrastructure.BookingProviders;

namespace WebApplication1.Resolvers;

public class CourtProviderResolver : ICourtProviderResolver
{
    private readonly IServiceProvider _serviceProvider;

    public CourtProviderResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ICourtProvider GetProvider(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.CourtMe => _serviceProvider.GetRequiredService<CourtMeProvider>(),
            ProviderType.KlubyOrg => _serviceProvider.GetRequiredService<KlubyOrgProvider>(),
            ProviderType.Playtomic => _serviceProvider.GetRequiredService<PlaytomicProvider>(),
            _ => throw new NotSupportedException("Provider not supported.")
        };
    }
}
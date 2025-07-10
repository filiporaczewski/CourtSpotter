using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface ICourtBookingProviderResolver
{
    ICourtBookingProvider GetProvider(ProviderType provider);
}
using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface ICourtBookingProviderResolver
{
    ICourtBookingProvider GetProvider(ProviderType provider);
}
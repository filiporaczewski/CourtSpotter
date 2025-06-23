using PadelCourts.Core.Models;

namespace PadelCourts.Core.Contracts;

public interface ICourtProviderResolver
{
    ICourtProvider GetProvider(ProviderType provider);
}
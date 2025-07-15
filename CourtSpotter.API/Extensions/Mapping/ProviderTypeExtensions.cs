using CourtSpotter.Core.Models;

namespace CourtSpotter.Extensions.Mapping;

public static class ProviderTypeExtensions
{
    public static string ToDisplayName(this ProviderType provider)
    {
        return provider switch
        {
            ProviderType.CourtMe => "CourtMe",
            ProviderType.KlubyOrg => "KlubyOrg",
            ProviderType.Playtomic => "Playtomic",
            ProviderType.RezerwujKort => "RezerwujKort",
            _ => throw new NotSupportedException($"Provider {provider} is not supported.")
        };
    }
}
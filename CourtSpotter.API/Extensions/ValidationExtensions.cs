using CourtSpotter.Filters;

namespace CourtSpotter.Extensions;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        return builder.AddEndpointFilter<EndpointValidationFilter<T>>();
    }
}
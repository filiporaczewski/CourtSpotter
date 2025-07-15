using CourtSpotter.Filters;

namespace CourtSpotter.Extensions.Endpoints;

public static class RouteHandlerExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class
    {
        return builder.AddEndpointFilter<EndpointValidationFilter<T>>();
    }
}
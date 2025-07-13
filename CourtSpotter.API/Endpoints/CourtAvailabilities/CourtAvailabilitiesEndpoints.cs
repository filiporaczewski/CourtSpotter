using CourtSpotter.Core.Contracts;
using CourtSpotter.Extensions;
using CourtSpotter.MappingExtensions;
using Microsoft.AspNetCore.Mvc;

namespace CourtSpotter.Endpoints.CourtAvailabilities;

public static class CourtAvailabilitiesEndpoints
{
    public static void MapCourtAvailabilitiesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/court-availabilities")
            .WithTags("Court Availabilities")
            .WithOpenApi();
        
        group.MapGet("/", GetCourtAvailabilities)
            .WithName("GetCourtAvailabilities")
            .WithSummary("Get court availabilities for a date range")
            .WithDescription("Retrieves all available court slots within the specified date range")
            .WithValidation<GetCourtAvailabilitiesQuery>();
    }
    
    private static async Task<IResult> GetCourtAvailabilities(
        [AsParameters] GetCourtAvailabilitiesQuery query,
        [FromServices] ICourtAvailabilityRepository repository, 
        CancellationToken cancellationToken = default)
    {
        var availabilities = await repository.GetAvailabilitiesAsync(
            query.StartDate.Date, 
            query.EndDate.Date.AddDays(1).AddTicks(-1),
            query.Durations,
            query.ClubIds,
            query.CourtType,
            cancellationToken);
            
        var dtos = availabilities.Select(a => a.ToDto()).ToList();

        return Results.Ok(new
        {
            TotalCount = dtos.Count,
            CourtAvailabilities = dtos
        });
    }
}
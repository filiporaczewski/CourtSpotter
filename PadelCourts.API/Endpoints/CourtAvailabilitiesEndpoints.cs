using Microsoft.AspNetCore.Mvc;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using WebApplication1.DTOs;

namespace WebApplication1.Endpoints;

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
            .WithDescription("Retrieves all available court slots within the specified date range");
    }
    
    private static async Task<IResult> GetCourtAvailabilities(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate,
        [FromQuery] int[]? durations,
        [FromQuery] string[]? clubIds,
        [FromQuery] CourtType? courtType,
        [FromServices] ICourtAvailabilityRepository repository, 
        CancellationToken cancellationToken = default)
    {
        var request = new GetCourtAvailabilitiesRequest(startDate, endDate);
        
        if (!request.IsValid)
        {
            return Results.BadRequest(new { Error = "End date must be greater than or equal to start date" });
        }
        
        if ((endDate - startDate).TotalDays > 14)
        {
            return Results.BadRequest(new { Error = "Date range cannot exceed 14 days" });
        }

        try
        {
            var availabilities = await repository.GetAvailabilitiesAsync(
                startDate.Date, 
                endDate.Date.AddDays(1).AddTicks(-1),
                durations,
                clubIds,
                courtType,
                cancellationToken);
            
            var dtos = availabilities.Select(MapToDto).ToList();

            return Results.Ok(new
            {
                TotalCount = dtos.Count,
                CourtAvailabilities = dtos
            });
        } 
        catch (Exception e)
        {
            return Results.Problem(
                title: "Error while getting court availabilities",
                detail: e.Message,
                statusCode: 500
            );
        }

    }
    
    private static CourtAvailabilityDto MapToDto(CourtAvailability courtAvailability)
    {
        return new CourtAvailabilityDto(
            Id: courtAvailability.Id ?? string.Empty,
            ClubName: courtAvailability.ClubName ?? "Unknown Club",
            CourtName: courtAvailability.CourtName ?? "Unknown Court",
            DateTime: courtAvailability.StartTime,
            Price: courtAvailability.Price,
            Currency: courtAvailability.Currency ?? "PLN",
            BookingUrl: courtAvailability.BookingUrl ?? string.Empty,
            Provider: MapProvider(courtAvailability.Provider),
            DurationInMinutes: CalculateDurationInMinutes(courtAvailability.StartTime, courtAvailability.EndTime),
            CourtType: courtAvailability.Type
        );
    }
    
    private static string MapProvider(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.CourtMe => "CourtMe",
            ProviderType.KlubyOrg => "KlubyOrg",
            ProviderType.Playtomic => "Playtomic",
            ProviderType.RezerwujKort => "RezerwujKort",
            _ => throw new NotSupportedException("Provider not supported.")
        };   
    }
    
    private static int CalculateDurationInMinutes(DateTime startTime, DateTime endTime)
    {
        return (int) (endTime - startTime).TotalMinutes;
    }
}
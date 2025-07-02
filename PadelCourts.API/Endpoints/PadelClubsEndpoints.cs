using Microsoft.AspNetCore.Mvc;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using WebApplication1.DTOs;

namespace WebApplication1.Endpoints;

public static class PadelClubsEndpoints
{
    public static void MapPadelClubsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/padel-clubs")
            .WithTags("Padel Clubs")
            .WithOpenApi();
        
        group.MapGet("/", GetPadelClubs)
            .WithName("GetPadelClubs")
            .WithSummary("Get all padel clubs")
            .WithDescription("Retrieves all padel clubs");
        
        group.MapPost("/", AddPadelClub)
            .WithName("AddPadelClub")
            .WithSummary("Add a new padel club")
            .WithDescription("Adds a new padel club to the database");
    }

    private static async Task<IResult> AddPadelClub([FromBody] AddPadelClubRequest request, [FromServices] IPadelClubsRepository repository, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.AddPadelClub(request.Name, request.Provider, cancellationToken);
            return Results.Ok();
        } catch (Exception e)
        {
            return Results.Problem(
                title: "Error while adding padel club",
                detail: e.Message,
                statusCode: 500
            );
        }
    }
    
    private static async Task<IResult> GetPadelClubs([FromServices] IPadelClubsRepository repository, CancellationToken cancellationToken = default)
    {
        try
        {
            var clubs = await repository.GetPadelClubs(cancellationToken);
        
            var dtos = clubs.Select(c => new PadelClubDto
            {
                ClubId = c.ClubId,
                Name = c.Name
            }).ToList();

            return Results.Ok(new
            {
                TotalCount = dtos.Count,
                Clubs = dtos
            });
        }
        catch (Exception e)
        {
            return Results.Problem(
                title: "Error while getting padel clubs",
                detail: e.Message,
                statusCode: 500
            );
        }
    }
}

public record AddPadelClubRequest(string Name, ProviderType Provider);
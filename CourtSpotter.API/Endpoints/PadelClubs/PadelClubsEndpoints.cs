using CourtSpotter.Core.Contracts;
using CourtSpotter.DTOs;
using CourtSpotter.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CourtSpotter.Endpoints.PadelClubs;

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
            .WithDescription("Adds a new padel club to the database")
            .WithValidation<AddPadelClubCommandValidator>();
    }

    private static async Task<IResult> AddPadelClub([FromBody] AddPadelClubCommand command, [FromServices] IPadelClubsRepository repository, CancellationToken cancellationToken = default)
    {
        await repository.AddPadelClub(command.Name, command.Provider, cancellationToken);
        return Results.Ok();
    }
    
    private static async Task<IResult> GetPadelClubs([FromServices] IPadelClubsRepository repository, CancellationToken cancellationToken = default)
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
}
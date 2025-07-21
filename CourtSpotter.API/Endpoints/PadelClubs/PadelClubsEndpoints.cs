using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.DTOs;
using CourtSpotter.Extensions;
using CourtSpotter.Extensions.DI;
using CourtSpotter.Extensions.Endpoints;
using CourtSpotter.Extensions.Mapping;
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
            .WithValidation<AddPadelClubCommand>()
            .RequireAuthorization("CreatePadelClubs");
    }

    private static async Task<IResult> AddPadelClub(
        [FromBody] AddPadelClubCommand command, 
        [FromServices] IPadelClubsRepository padelClubsRepository, 
        [FromServices] IPlaytomicCourtsRepository playtomicCourtsRepository, 
        [FromServices] IPlaytomicCourtsSyncManager playtomicCourtsSyncManager, 
        CancellationToken cancellationToken = default)
    {
        var clubId = string.Empty;
        
        if (command.Provider == ProviderType.Playtomic)
        {
            var playtomicCourts = await playtomicCourtsSyncManager.RetrievePlaytomicCourts(command.Name, cancellationToken);

            if (playtomicCourts.Any())
            {
                await playtomicCourtsRepository.AddPlaytomicCourts(playtomicCourts, cancellationToken);
                clubId = playtomicCourts.First().ClubId;
            }
            else
            {
                return Results.BadRequest("No courts found for specified playtomic club. Club probably do not exist.");
            }
        }

        if (string.IsNullOrEmpty(clubId))
        {
            clubId = Guid.NewGuid().ToString();
        }
        
        var newClub = PadelClub.Create(clubId, command.Name, command.Provider, command.TimeZone, command.PagesCount);;
        await padelClubsRepository.AddPadelClub(newClub, command.Provider, cancellationToken);
        
        return Results.Ok(new
        {
            Id = newClub.ClubId,
        });
    }
    
    private static async Task<IResult> GetPadelClubs([FromServices] IPadelClubsRepository repository, CancellationToken cancellationToken = default)
    {
        var clubs = await repository.GetPadelClubs(cancellationToken);
    
        var dtos = clubs.Select(c => new PadelClubDto
        {
            ClubId = c.ClubId,
            Name = c.Name,
            Provider = c.Provider.ToDisplayName(),
            PagesCount = c.PagesCount,
            TimeZone = c.TimeZone
        }).ToList();

        return Results.Ok(new
        {
            TotalCount = dtos.Count,
            Clubs = dtos
        });
    }
}
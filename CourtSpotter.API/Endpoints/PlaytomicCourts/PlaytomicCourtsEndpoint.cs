using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Endpoints.PlaytomicCourts;
using CourtSpotter.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CourtSpotter.Endpoints;

public static class PlaytomicCourtsEndpoint
{
    public static void MapPlaytomicCourtsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/playtomic-courts")
            .WithTags("Playtomic Courts")
            .WithOpenApi();

        group.MapPost("/sync", SyncPlaytomicClub)
            .WithName("SyncPlaytomicCourts")
            .WithSummary("Sync playtomic courts for a given club")
            .WithValidation<SyncPlaytomicClubCommandValidator>();
    }

    private static async Task<IResult> SyncPlaytomicClub(
        [FromBody] SyncPlaytomicClubCommand command,
        [FromServices] IPadelClubsRepository padelClubsRepository, 
        [FromServices] IPlaytomicCourtsSyncManager playtomicCourtsSyncManager,
        [FromServices] IPlaytomicCourtsRepository playtomicCourtsRepository,
        CancellationToken cancellationToken = default)
    {
        var club = await padelClubsRepository.GetByName(command.ClubName, cancellationToken);

        if (club is not { Provider: ProviderType.Playtomic })
        {
            return Results.BadRequest("Club is not a Playtomic club.");
        }

        var playtomicCourts = await playtomicCourtsSyncManager.RetrievePlaytomicCourts(club, cancellationToken);
        await playtomicCourtsRepository.AddPlaytomicCourts(playtomicCourts, cancellationToken);
        return Results.Ok(playtomicCourts);
    }
}
using Microsoft.AspNetCore.Mvc;
using PadelCourts.Core.Contracts;

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
            .WithSummary("Sync playtomic courts for a given club");
    }

    private static async Task<IResult> SyncPlaytomicClub(
        [FromBody] string clubName,
        [FromServices] IPadelClubsRepository padelClubsRepository, 
        [FromServices] IPlaytomicCourtsSyncManager playtomicCourtsSyncManager,
        [FromServices] IPlaytomicCourtsRepository playtomicCourtsRepository,
        CancellationToken cancellationToken = default)
    {
        var club = await padelClubsRepository.GetByName(clubName, cancellationToken);

        if (club is null)
        {
            return Results.NotFound();
        }

        var playtomicCourts = await playtomicCourtsSyncManager.RetrievePlaytomicCourts(club, cancellationToken);

        foreach (var court in playtomicCourts)
        {
            await playtomicCourtsRepository.AddPlaytomicCourt(court, cancellationToken);
        }
        
        return Results.Ok(playtomicCourts);
    }
}
using CourtSpotter.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CourtSpotter.Endpoints.CourtAvailabilities;

public record GetCourtAvailabilitiesQuery(
    [FromQuery] DateTime StartDate,
    [FromQuery] DateTime EndDate,
    [FromQuery] int[]? Durations = null,
    [FromQuery] string[]? ClubIds = null,
    [FromQuery] CourtType? CourtType = null
);
namespace CourtSpotter.Core.Models;

public record KlubyOrgScheduleParserResult(List<CourtAvailability> CourtAvailabilities, bool Success, string? ErrorMessage);
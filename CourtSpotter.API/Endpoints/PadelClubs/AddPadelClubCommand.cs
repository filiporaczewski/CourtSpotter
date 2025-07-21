using CourtSpotter.Core.Models;
namespace CourtSpotter.Endpoints.PadelClubs;

public record AddPadelClubCommand(string Name, ProviderType Provider, string TimeZone = "Europe/Warsaw", int? PagesCount = null);
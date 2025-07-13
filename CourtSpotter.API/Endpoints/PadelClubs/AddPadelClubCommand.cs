using CourtSpotter.Core.Models;
namespace CourtSpotter.Endpoints.PadelClubs;

public record AddPadelClubCommand(string Name, ProviderType Provider, int? PagesCount = null);
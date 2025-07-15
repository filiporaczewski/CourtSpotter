using CourtSpotter.Core.Models;
using CourtSpotter.DTOs;

namespace CourtSpotter.Extensions.Mapping;

public static class CourtAvailabilityMappingExtensions
{
    public static CourtAvailabilityDto ToDto(this CourtAvailability courtAvailability)
    {
        return new CourtAvailabilityDto(
            Id: courtAvailability.Id ?? string.Empty,
            ClubName: courtAvailability.ClubName ?? "Unknown Club",
            CourtName: courtAvailability.CourtName ?? "Unknown Court",
            DateTime: courtAvailability.StartTime,
            Price: courtAvailability.Price,
            Currency: courtAvailability.Currency ?? "PLN",
            BookingUrl: courtAvailability.BookingUrl ?? string.Empty,
            Provider: courtAvailability.Provider.ToDisplayName(),
            DurationInMinutes: (int)courtAvailability.Duration.TotalMinutes,
            CourtType: courtAvailability.Type
        );
    }
}
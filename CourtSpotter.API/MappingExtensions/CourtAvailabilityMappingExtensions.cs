using CourtSpotter.Core.Models;
using CourtSpotter.DTOs;
using CourtSpotter.Extensions;

namespace CourtSpotter.MappingExtensions;

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
            DurationInMinutes: courtAvailability.StartTime.CalculateDurationInMinutes(courtAvailability.EndTime),
            CourtType: courtAvailability.Type
        );
    }
}
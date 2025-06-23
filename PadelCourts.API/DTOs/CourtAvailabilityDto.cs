namespace WebApplication1.DTOs;

public record CourtAvailabilityDto(
    string Id,
    string ClubName,
    string CourtName,
    DateTime DateTime,
    decimal Price,
    string Currency,
    string BookingUrl,
    string Provider,
    int DurationInMinutes
);
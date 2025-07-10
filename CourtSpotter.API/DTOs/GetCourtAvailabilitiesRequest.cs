using System.ComponentModel.DataAnnotations;

namespace CourtSpotter.DTOs;

public record GetCourtAvailabilitiesRequest(
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
)
{
    public bool IsValid => EndDate > StartDate;
}
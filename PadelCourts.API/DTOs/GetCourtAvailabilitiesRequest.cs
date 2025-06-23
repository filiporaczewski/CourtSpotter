using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs;

public record GetCourtAvailabilitiesRequest(
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
)
{
    public bool IsValid => EndDate > StartDate;
}
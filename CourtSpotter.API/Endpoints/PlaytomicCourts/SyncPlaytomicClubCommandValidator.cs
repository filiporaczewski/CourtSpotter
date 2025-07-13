using FluentValidation;

namespace CourtSpotter.Endpoints.PlaytomicCourts;

public class SyncPlaytomicClubCommandValidator : AbstractValidator<SyncPlaytomicClubCommand>
{
    public SyncPlaytomicClubCommandValidator()
    {
        RuleFor(x => x.ClubName).NotEmpty().WithMessage("Club name is required");
    }
}
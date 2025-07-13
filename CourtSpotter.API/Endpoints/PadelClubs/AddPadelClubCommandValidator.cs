using FluentValidation;

namespace CourtSpotter.Endpoints.PadelClubs;

public class AddPadelClubCommandValidator : AbstractValidator<AddPadelClubCommand>
{
    public AddPadelClubCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name must not be empty");
        RuleFor(x => x.Provider).NotEmpty().WithMessage("Provider must not be empty");
    }
}
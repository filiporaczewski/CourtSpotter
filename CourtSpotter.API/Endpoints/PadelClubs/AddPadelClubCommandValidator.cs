using FluentValidation;

namespace CourtSpotter.Endpoints.PadelClubs;

public class AddPadelClubCommandValidator : AbstractValidator<AddPadelClubCommand>
{
    public AddPadelClubCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name must not be empty");
        RuleFor(x => x.Provider).IsInEnum().WithMessage("Provider must not be empty");
        RuleFor(x => x.TimeZone).NotEmpty().Must(IsValidTimeZone).WithMessage("Time zone must be a valid time zone ID");
    }
    
    private bool IsValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
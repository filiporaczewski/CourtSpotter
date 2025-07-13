using CourtSpotter.BackgroundServices;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace CourtSpotter.Endpoints.CourtAvailabilities;

public class GetCourtAvailabilitiesQueryValidator : AbstractValidator<GetCourtAvailabilitiesQuery>
{
    public GetCourtAvailabilitiesQueryValidator(IOptions<CourtBookingAvailabilitiesSyncOptions> syncOptions)
    {
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required");
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required");
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must be before end date");
        RuleFor(x => x).Must(x => (x.EndDate - x.StartDate).TotalDays <= syncOptions.Value.DaysToSyncCount)
            .WithMessage($"Maximum number of days to sync is {syncOptions.Value.DaysToSyncCount} days");
    }
}
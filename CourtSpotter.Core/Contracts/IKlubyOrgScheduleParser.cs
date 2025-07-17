using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Contracts;

public interface IKlubyOrgScheduleParser
{
    Task<KlubyOrgScheduleParserResult> ParseScheduleAsync(
        string htmlContent, 
        DateTime date, 
        PadelClub padelClub, 
        string scheduleUrl,
        CancellationToken cancellationToken = default);
}
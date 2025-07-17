using AngleSharp;
using AngleSharp.Dom;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Microsoft.Extensions.Options;

namespace CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;

public class KlubyOrgScheduleParser : IKlubyOrgScheduleParser
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _localTimeZone;
    private readonly string _baseUrl;
    private const int MinSlotsForOneHour = 2;
    private const int MinSlotsForOneAndHalfHours = 3;
    private const int MinSlotsForTwoHours = 4;

    public KlubyOrgScheduleParser(TimeProvider timeProvider, IOptions<KlubyOrgProviderOptions> options)
    {
        _timeProvider = timeProvider;
        _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.LocalTimeZoneId);
        _baseUrl = options.Value.BaseUrl;
    }
    
    public async Task<KlubyOrgScheduleParserResult> ParseScheduleAsync(string htmlContent, DateTime date, PadelClub padelClub, string scheduleUrl, CancellationToken cancellationToken = default)
    {
        var dailyAvailabilities = new List<CourtAvailability>();
        var bookingScheduleDomDocument = await GetBookingScheduleHtmlDocumentAsync(htmlContent, cancellationToken);
        var bookingScheduleTableElement = bookingScheduleDomDocument.QuerySelector("#grafik");
        
        if (bookingScheduleTableElement is null)
        {
            return new KlubyOrgScheduleParserResult(dailyAvailabilities, false, "Failed to find booking schedule table");
        }
        
        var availableCourtNames = bookingScheduleTableElement.QuerySelectorAll("thead tr th")
            .Skip(1) 
            .Select(th => ExtractCourtName(th.TextContent))
            .ToList();
        
        var halfHourSlotRows = bookingScheduleTableElement.QuerySelectorAll("tbody tr, tr:not(thead tr)").ToList();
        
        if (!halfHourSlotRows.Any())
        {
            return new KlubyOrgScheduleParserResult(dailyAvailabilities, false, "Failed to find any half hour slot rows");
        }
        
        var courtStates = new GridColumnState[availableCourtNames.Count];

        for (var courtColIndex = 0; courtColIndex < availableCourtNames.Count; courtColIndex++)
        {
            courtStates[courtColIndex] = new GridColumnState
            {
                CourtName = availableCourtNames[courtColIndex]
            };
        }

        for (var rowIndex = 0; rowIndex < halfHourSlotRows.Count; rowIndex++)
        {
            var row = halfHourSlotRows[rowIndex];
            var rowCells = row.QuerySelectorAll("td").ToList();
            var timeText = rowCells[0].TextContent.Trim();
            
            if (!TimeSpan.TryParse(timeText, out var rowTime))
            {
                continue;
            }
            
            var currentCellIndex = 1;

            for (var colIndex = 0; colIndex < courtStates.Length; colIndex++)
            {
                var courtState = courtStates[colIndex];
                var isBookingBlockedForCurrentCourt = courtState.RemainingRowsBlocked > 0;
                
                if (isBookingBlockedForCurrentCourt)
                {
                    courtState.RemainingRowsBlocked--;
                }
                else
                {
                    if (currentCellIndex >= rowCells.Count)
                    {
                        break;
                    }
                    
                    var cell = rowCells[currentCellIndex];
                    var rowspan = int.TryParse(cell.GetAttribute("rowspan"), out var rs) ? rs : 1;
                    currentCellIndex++;
                    
                    if (rowspan > 1)
                    {
                        var canCurrentStateBeBookedForAtLeastOneHour = courtState.ConsecutiveAvailableRows >= 2;
                        
                        if (canCurrentStateBeBookedForAtLeastOneHour)
                        {
                            dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, scheduleUrl));
                        }
                        
                        courtState.StartTime = null;
                        courtState.ConsecutiveAvailableRows = 0;
                        courtState.RemainingRowsBlocked = rowspan - 1;
                    }
                    else
                    {
                        if (cell.QuerySelector("a[href*='rezerwuj']") != null)
                        {
                            if (courtState.ConsecutiveAvailableRows == 0)
                            {
                                courtState.StartTime = rowTime;
                            }
                            
                            courtState.ConsecutiveAvailableRows++;   
                        }
                        else
                        {
                            if (courtState.ConsecutiveAvailableRows >= MinSlotsForOneHour)
                            {
                                dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, scheduleUrl));  
                            }
                            
                            courtState.StartTime = null;
                            courtState.ConsecutiveAvailableRows = 0;
                        }
                    }
                }
            }
        }
        
        for (var colIndex = 0; colIndex < courtStates.Length; colIndex++)
        {
            var courtState = courtStates[colIndex];
            
            if (courtState.ConsecutiveAvailableRows >= MinSlotsForOneHour)
            {
                dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, scheduleUrl)); 
            }
        }

        return new KlubyOrgScheduleParserResult(dailyAvailabilities, true, null);;
    }
    
    private static string ExtractCourtName(string headerText)
    {
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return "Unknown Court";
        }
        
        var cleanedText = headerText.Replace("\t", "");
        var lines = cleanedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.FirstOrDefault()?.Trim() ?? "Unknown Court";
    }

    private async Task<IDocument> GetBookingScheduleHtmlDocumentAsync(string bookingScheduleHtmlString, CancellationToken cancellationToken)
    {
        var angleSharpConfig = Configuration.Default;
        var browsingContext = BrowsingContext.New(angleSharpConfig);
        return await browsingContext.OpenAsync(req => req.Content(bookingScheduleHtmlString), cancel: cancellationToken);
    }
    
        private List<CourtAvailability> GenerateCourtAvailabilities(DateTime date, GridColumnState courtState, PadelClub padelClub, string courtScheduleUrl)
    {
        var courtAvailabilities = new List<CourtAvailability>();
        
        // Convert current UTC time to local time zone for filtering past slots
        var currentUtcTime = _timeProvider.GetUtcNow();
        var currentLocalTime = TimeZoneInfo.ConvertTimeFromUtc(currentUtcTime.DateTime, _localTimeZone);

        if (courtState.StartTime is null)
        {
            return courtAvailabilities;
        }
        
        var availabilityDate = date.Add(courtState.StartTime.Value);
        var availableHalfHourSlots = courtState.ConsecutiveAvailableRows;

        for (int currentSlotIndex = 0; currentSlotIndex < courtState.ConsecutiveAvailableRows; currentSlotIndex++)
        {
            if (availabilityDate < currentLocalTime)
            {
                break;
            }

            if (availableHalfHourSlots >= MinSlotsForTwoHours)
            {
                courtAvailabilities.Add(GenerateCourtAvailabilityTwoHours(padelClub, availabilityDate, courtState.CourtName, courtScheduleUrl));
            }

            if (availableHalfHourSlots >= MinSlotsForOneAndHalfHours)
            {
                courtAvailabilities.Add(GenerateCourtAvailabilityOneAndHalfHours(padelClub, availabilityDate, courtState.CourtName, courtScheduleUrl));
            }

            if (availableHalfHourSlots >= MinSlotsForOneHour)
            {
                courtAvailabilities.Add(GenerateCourtAvailabilityOneHour(padelClub, availabilityDate, courtState.CourtName, courtScheduleUrl));
            }

            availableHalfHourSlots--;
            availabilityDate = availabilityDate.AddMinutes(30);
        }
        
        return courtAvailabilities;
    }

    private CourtAvailability GenerateCourtAvailabilityOneHour(PadelClub padelClub, DateTime startDate, string courtName, string bookingUrl)
    {
        return GenerateCourtAvailability(padelClub, startDate, 60, courtName, bookingUrl);
    }
    
    private CourtAvailability GenerateCourtAvailabilityTwoHours(PadelClub padelClub, DateTime startDate, string courtName, string bookingUrl)
    {
        return GenerateCourtAvailability(padelClub, startDate, 120, courtName, bookingUrl);
    }
    
    private CourtAvailability GenerateCourtAvailabilityOneAndHalfHours(PadelClub padelClub, DateTime startDate, string courtName, string bookingUrl)
    {
        return GenerateCourtAvailability(padelClub, startDate, 90, courtName, bookingUrl);
    }

    private CourtAvailability GenerateCourtAvailability(PadelClub padelClub, DateTime startDate, int durationInMinutes, string courtName, string bookingUrl)
    {
        var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(startDate, _localTimeZone);
        var utcEndTime = utcStartTime.AddMinutes(durationInMinutes);
        
        return new CourtAvailability
        {
            ClubId = padelClub.ClubId,
            CourtName = courtName,
            ClubName = padelClub.Name,
            Provider = ProviderType.KlubyOrg,
            StartTime = utcStartTime,
            EndTime = utcEndTime,
            Type = IsIndoor(courtName) ? CourtType.Indoor : CourtType.Outdoor,
            BookingUrl = $"{_baseUrl}/{bookingUrl}",
            Currency = "PLN",
            Price = 0,
            Id = Guid.NewGuid().ToString()
        };   
    }

    private static bool IsIndoor(string courtName) => courtName.Contains("hala", StringComparison.InvariantCultureIgnoreCase);
}
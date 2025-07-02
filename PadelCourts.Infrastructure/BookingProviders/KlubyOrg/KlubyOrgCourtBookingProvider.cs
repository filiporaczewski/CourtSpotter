using System.Globalization;
using System.Net;
using AngleSharp;
using AngleSharp.Dom;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Infrastructure.BookingProviders.KlubyOrg;

public class KlubyOrgCourtBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _authenticatedClient;
    private readonly string _baseUrl = "https://kluby.org/";
    private readonly string _klubyOrgLogin = "mati.lewandowski1243@gmail.com";
    private readonly string _klubyOrgPassword = "Subaru23";
    private const int MinSlotsForOneHour = 2;
    private const int MinSlotsForOneAndHalfHours = 3;
    private const int MinSlotsForTwoHours = 4;
    private readonly CookieContainer _cookieContainer;

    public KlubyOrgCourtBookingProvider(IHttpClientFactory httpClientFactory, CookieContainer cookieContainer)
    {
        _authenticatedClient = httpClientFactory.CreateClient("KlubyOrgClient");
        _cookieContainer = cookieContainer;
    }
    
    public async Task<IEnumerable<CourtAvailability>> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allAvailabilities = new List<CourtAvailability>();
        await EnsureAuthenticatedAsync(cancellationToken);
        
        var dateTasks = new List<Task<IEnumerable<CourtAvailability>>>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            // var dailyAvailabilities = await GetDailyAvailabilitiesAsync(padelClub, date, cancellationToken);
            // allAvailabilities.AddRange(dailyAvailabilities);

            if (padelClub.PagesCount is > 0)
            {
                for (int page = 0; page < padelClub.PagesCount; page++)
                {
                    dateTasks.Add(GetDailyAvailabilitiesAsync(padelClub, date, page, cancellationToken));
                }
            }
            else
            {
                dateTasks.Add(GetDailyAvailabilitiesAsync(padelClub, date, 0, cancellationToken));
            }
        }
        
        var dailyAvailabilitiesResults = await Task.WhenAll(dateTasks);
        
        foreach (var dailyAvailabilities in dailyAvailabilitiesResults)
        {
            allAvailabilities.AddRange(dailyAvailabilities);
        }
        
        return allAvailabilities;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var cookies = _cookieContainer.GetCookies(new Uri(_baseUrl));
        
        if (cookies.Any() && cookies.All(c => c.Name != "kluby_org"))
        {
            await LoginToKlubyOrgAsync(_klubyOrgLogin, _klubyOrgPassword, cancellationToken);
        }
    }

    private async Task<IEnumerable<CourtAvailability>> GetDailyAvailabilitiesAsync(PadelClub padelClub, DateTime date, int page, CancellationToken cancellationToken = default)
    {
        var dailyAvailabilities = new List<CourtAvailability>();
        var dateBookingScheduleUrl = GetDateBookingScheduleUrl(date, padelClub.Name, page);

        try
        {
            var bookingScheduleHtmlString = await GetBookingScheduleHtmlStringAsync(dateBookingScheduleUrl, cancellationToken);
            var bookingScheduleDomDocument = await GetBookingScheduleHtmlDocumentAsync(bookingScheduleHtmlString, cancellationToken);

            var bookingScheduleTableElement = bookingScheduleDomDocument.QuerySelector("#grafik");
            
            if (bookingScheduleTableElement is null)
            {
                return dailyAvailabilities;
            }
            
            var availableCourtNames = bookingScheduleTableElement.QuerySelectorAll("thead tr th")
                .Skip(1) 
                .Select(th => ExtractCourtName(th.TextContent))
                .ToList();
            
            var halfHourSlotRows = bookingScheduleTableElement.QuerySelectorAll("tbody tr, tr:not(thead tr)").ToList();
            
            if (!halfHourSlotRows.Any())
            {
                return dailyAvailabilities;
            }

            var courtStates = new GridColumnState[availableCourtNames.Count];

            for (int courtColIndex = 0; courtColIndex < availableCourtNames.Count; courtColIndex++)
            {
                courtStates[courtColIndex] = new GridColumnState
                {
                    CourtName = availableCourtNames[courtColIndex]
                };
            }

            for (int rowIndex = 0; rowIndex < halfHourSlotRows.Count; rowIndex++)
            {
                var row = halfHourSlotRows[rowIndex];
                var rowCells = row.QuerySelectorAll("td").ToList();
                var timeText = rowCells[0].TextContent.Trim();
                
                if (!TimeSpan.TryParse(timeText, out var rowTime))
                {
                    continue;
                }

                var currentCellIndex = 1;
                
                for (int colIndex = 0; colIndex < courtStates.Length; colIndex++)
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
                                dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, dateBookingScheduleUrl));
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
                                    dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, dateBookingScheduleUrl));  
                                }
                                
                                courtState.StartTime = null;
                                courtState.ConsecutiveAvailableRows = 0;
                            }
                        }
                    }
                }
            }
            
            for (int colIndex = 0; colIndex < courtStates.Length; colIndex++)
            {
                var courtState = courtStates[colIndex];
                
                if (courtState.ConsecutiveAvailableRows >= MinSlotsForOneHour)
                {
                    dailyAvailabilities.AddRange(GenerateCourtAvailabilities(date, courtState, padelClub, dateBookingScheduleUrl)); 
                }
            }
        } catch (Exception e)
        {
            Console.WriteLine($"Error fetching KlubyOrg availability: {e.Message}");
        }

        return dailyAvailabilities;
    }

    private async Task<IDocument> GetBookingScheduleHtmlDocumentAsync(string bookingScheduleHtmlString, CancellationToken cancellationToken = default)
    {
        var angleSharpConfig = Configuration.Default;
        var browsingContext = BrowsingContext.New(angleSharpConfig);
        return await browsingContext.OpenAsync(req => req.Content(bookingScheduleHtmlString), cancel: cancellationToken);
    }
    
    private async Task<string> GetBookingScheduleHtmlStringAsync(string dateBookingScheduleUrl, CancellationToken cancellationToken = default)
    {
        var clientResponse = await _authenticatedClient.GetStringAsync(dateBookingScheduleUrl, cancellationToken);

        if (clientResponse.Contains("Wyloguj"))
        {
            return clientResponse;
        }
        
        await LoginToKlubyOrgAsync(_klubyOrgLogin, _klubyOrgPassword, cancellationToken);
        return await _authenticatedClient.GetStringAsync(dateBookingScheduleUrl, cancellationToken);
    }

    private string GetDateBookingScheduleUrl(DateTime date, string clubName, int page=0)
    {
        var formattedClubName = clubName.ToLowerInvariant().Replace(" ", "-");
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{formattedClubName}/grafik?data_grafiku={dateString}&dyscyplina=4&strona={page}";
    }

    private string ExtractCourtName(string headerText)
    {
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return "Unknown Court";
        }
        
        var cleanedText = headerText.Replace("\t", "");
        var lines = cleanedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines.FirstOrDefault()?.Trim() ?? "Unknown Court";
    }

    private List<CourtAvailability> GenerateCourtAvailabilities(DateTime date, GridColumnState courtState, PadelClub padelClub, string courtScheduleUrl)
    {
        var courtAvailabilities = new List<CourtAvailability>();
        var currentDate = DateTime.Now;

        if (courtState.StartTime is null)
        {
            return courtAvailabilities;
        }
        
        var availabilityDate = date.Add(courtState.StartTime.Value);
        var availableHalfHourSlots = courtState.ConsecutiveAvailableRows;

        for (int currentSlotIndex = 0; currentSlotIndex < courtState.ConsecutiveAvailableRows; currentSlotIndex++)
        {
            if (availabilityDate < currentDate)
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

    private CourtAvailability GenerateCourtAvailability(PadelClub padelClub, DateTime startDate, int durationInMinutes,
        string courtName, string bookingUrl)
    {
        return new CourtAvailability
        {
            ClubId = padelClub.ClubId,
            CourtName = courtName,
            ClubName = padelClub.Name,
            Provider = ProviderType.KlubyOrg,
            StartTime = startDate,
            EndTime = startDate.AddMinutes(durationInMinutes),
            Type = IsIndoor(courtName) ? CourtType.Indoor : CourtType.Outdoor,
            BookingUrl = $"{_baseUrl}/{bookingUrl}",
            Currency = "PLN",
            Price = 0,
            Id = Guid.NewGuid().ToString()
        };   
    }


    private async Task LoginToKlubyOrgAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var loginParams = new KeyValuePair<string, string>[]
        {
            new ("konto", username),
            new ("haslo", password),
            new ("logowanie", "1"),
            new ("remember", "1"),
            new ("page", "/")
        };
        
        await _authenticatedClient.PostAsync("logowanie", new FormUrlEncodedContent(loginParams), cancellationToken);
    }

    private bool IsIndoor(string courtName)
    {
        return courtName.ToLowerInvariant().Contains("hala");
    }
}
using System.Globalization;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Results;

public class KlubyOrgCourtBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _httpClient;
    private readonly IKlubyOrgAuthenticationService _authenticationService;
    private readonly IKlubyOrgScheduleParser _scheduleParser;

    public KlubyOrgCourtBookingProvider(IHttpClientFactory httpClientFactory, IKlubyOrgAuthenticationService authenticationService, IKlubyOrgScheduleParser scheduleParser)
    {
        _httpClient = httpClientFactory.CreateClient("KlubyOrgClient");
        _authenticationService = authenticationService;
        _scheduleParser = scheduleParser;
    }
    
    public async Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allAvailabilities = new List<CourtAvailability>();
        var failedDailyResults = new List<FailedDailyCourtBookingAvailabilitiesSyncResult>();
        
        try
        {
            var isAuthenticated = await _authenticationService.EnsureAuthenticatedAsync(cancellationToken);
            
            if (!isAuthenticated)
            {
                return CreateNonAuthenticatedSyncResult(startDate, endDate, "Failed to authenticate to kluby.org", null);
            }
        } 
        catch (Exception ex)
        {
            return CreateNonAuthenticatedSyncResult(startDate, endDate, null, ex);
        }
        
        var dateTasks = new List<Task<DailyCourtBookingAvailabilitiesSyncResult>>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
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
        
        var dailyResults = await Task.WhenAll(dateTasks);
            
        foreach (var dailyResult in dailyResults)
        {
            if (dailyResult.Success)
            {
                allAvailabilities.AddRange(dailyResult.CourtAvailabilities);
            }
            else
            {
                failedDailyResults.Add(new FailedDailyCourtBookingAvailabilitiesSyncResult
                {
                    Date = dailyResult.Date,
                    Reason = dailyResult.FailureReason!,
                    Exception = dailyResult.Exception
                });
            }
        }
            
        return new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = allAvailabilities,
            FailedDailyCourtBookingAvailabilitiesSyncResults = failedDailyResults
        };
    }

    private async Task<DailyCourtBookingAvailabilitiesSyncResult> GetDailyAvailabilitiesAsync(PadelClub padelClub, DateTime date, int page, CancellationToken cancellationToken = default)
    {
        var dateBookingScheduleUrl = GetDateBookingScheduleUrl(date, padelClub.Name, page);

        try
        {
            var bookingScheduleHtmlString = await GetBookingScheduleHtmlStringAsync(dateBookingScheduleUrl, cancellationToken);
            
            if (string.IsNullOrEmpty(bookingScheduleHtmlString))
            {
                return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Empty response from KlubyOrg");
            }
            
            var scheduleParseResult = await _scheduleParser.ParseScheduleAsync(
                bookingScheduleHtmlString, 
                date, 
                padelClub, 
                dateBookingScheduleUrl, 
                cancellationToken);
            
            return new DailyCourtBookingAvailabilitiesSyncResult
            {
                CourtAvailabilities = scheduleParseResult.CourtAvailabilities,
                Date = DateOnly.FromDateTime(date),
                Success = scheduleParseResult.Success,
                FailureReason = scheduleParseResult.ErrorMessage ?? null
            };
        }
        catch (HttpRequestException ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Network error calling KlubyOrg", ex);
        }
        catch (TaskCanceledException ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Request timeout calling KlubyOrg", ex);
        }
        catch (Exception ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Unexpected error processing KlubyOrg", ex);
        }
    }
    
    private async Task<string> GetBookingScheduleHtmlStringAsync(string dateBookingScheduleUrl, CancellationToken cancellationToken = default) => await _httpClient.GetStringAsync(dateBookingScheduleUrl, cancellationToken);

    private string GetDateBookingScheduleUrl(DateTime date, string clubName, int page=0)
    {
        var formattedClubName = clubName.ToLowerInvariant().Replace(" ", "-");
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{formattedClubName}/grafik?data_grafiku={dateString}&dyscyplina=4&strona={page}";
    }

    private CourtBookingAvailabilitiesSyncResult CreateNonAuthenticatedSyncResult(DateTime startDate, DateTime endDate, string? reason, Exception? exception)
    {
        var availabilities = new List<CourtAvailability>();
        var failedDailyResults = new List<FailedDailyCourtBookingAvailabilitiesSyncResult>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            failedDailyResults.Add(new FailedDailyCourtBookingAvailabilitiesSyncResult
            {
                Date = DateOnly.FromDateTime(date),
                Reason = reason ?? "Error authenticating to kluby.org",
                Exception = exception
            });
        }
            
        return new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = availabilities,
            FailedDailyCourtBookingAvailabilitiesSyncResults = failedDailyResults
        };
    }
}
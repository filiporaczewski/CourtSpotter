using System.Globalization;
using System.Text.Json;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Options;
using CourtSpotter.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CourtSpotter.Infrastructure.BookingProviders.Playtomic;

public class PlaytomicBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _earliestPossibleBookingHour;
    private readonly int _latestPossibleBookingHour;
    private readonly string _baseUrl;

    public PlaytomicBookingProvider(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, IOptions<PlaytomicProviderOptions> playtomicOptions, IOptions<CourtBookingAvailabilitiesSyncOptions> syncOptions)
    {
        _httpClient = httpClientFactory.CreateClient("PlaytomicClient");
        _serviceProvider = serviceProvider;
        _earliestPossibleBookingHour = syncOptions.Value.EarliestBookingHour;
        _latestPossibleBookingHour = syncOptions.Value.LatestBookingHour;
        _baseUrl = playtomicOptions.Value.ApiBaseUrl;
    }
    
    public async Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allAvailabilities = new List<CourtAvailability>();
        var failedDailyResults = new List<FailedDailyCourtBookingAvailabilitiesSyncResult>();

        using var scope = _serviceProvider.CreateScope();
        var playtomicCourtsRepository = scope.ServiceProvider.GetRequiredService<IPlaytomicCourtsRepository>();
        var bookableCourts = await playtomicCourtsRepository.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken);
        
        var dateTasks = new List<Task<DailyCourtBookingAvailabilitiesSyncResult>>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dateTasks.Add(GetCourtAvailabilitiesSingleDay(padelClub, date, bookableCourts, cancellationToken));
        }
        
        var dailyAvailabilitiesResults = await Task.WhenAll(dateTasks);

        foreach (var dailyResult in dailyAvailabilitiesResults)
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

    private async Task<DailyCourtBookingAvailabilitiesSyncResult> GetCourtAvailabilitiesSingleDay(PadelClub padelClub, DateTime date, IEnumerable<PlaytomicCourt> bookableCourts, CancellationToken cancellationToken = default)
    {
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dailyAvailabilitiesBookingEndpointUrl = BuildAvailabilityUrl(_baseUrl, padelClub.ClubId, dateString);

        try
        { 
            var dailyAvailabilitiesBookingEndpointResponse = await _httpClient.GetStringAsync(dailyAvailabilitiesBookingEndpointUrl, cancellationToken);
            
            if (string.IsNullOrEmpty(dailyAvailabilitiesBookingEndpointResponse))
            {
                return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Empty response from Playtomic API");
            }
            
            var tenantAvailabilities = JsonSerializer.Deserialize<List<TenantAvailability>>(dailyAvailabilitiesBookingEndpointResponse);
            var courtAvailabilities = new List<CourtAvailability>();

            if (tenantAvailabilities is null)
            {
                return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Failed to deserialize response from Playtomic API");
            }
            
            foreach (var tenantAvailability in tenantAvailabilities)
            {
                var court = bookableCourts.FirstOrDefault(c => c.Id == tenantAvailability.ResourceId);
                
                if (court is null)
                {
                    continue;
                }
                
                var padelClubTimeZone = TimeZoneInfo.FindSystemTimeZoneById(padelClub.TimeZone);
                
                var filteredSlots = tenantAvailability.Slots.Where(slot => IsValidTimeSlot(slot, date, padelClubTimeZone)).ToList();

                foreach (var slot in filteredSlots)
                {
                    var (startDateTime, endDateTime) = ParseSlotTimes(slot, date);
                    var price = GetPriceAndCurrency(slot.Price);

                    if (!startDateTime.HasValue || !endDateTime.HasValue)
                    {
                        continue;
                    }

                    courtAvailabilities.Add(new CourtAvailability
                    {
                        ClubId = padelClub.ClubId,
                        Provider = ProviderType.Playtomic,
                        BookingUrl = $"https://playtomic.com/clubs/{GetUrlSuffix(padelClub.Name)}",
                        StartTime = startDateTime.Value,
                        EndTime = endDateTime.Value,
                        Price = price.Item1,
                        Currency = price.Item2,
                        Id = Guid.NewGuid().ToString(),
                        ClubName = padelClub.Name,
                        Type = court.Type,
                        CourtName = court.Name,
                    });
                }
            }

            return new DailyCourtBookingAvailabilitiesSyncResult
            {
                CourtAvailabilities = courtAvailabilities,
                Date = DateOnly.FromDateTime(date),
                Success = true
            };
        }
        catch (HttpRequestException ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Network error calling Playtomic API", ex);
        }
        catch (TaskCanceledException ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Request timeout calling Playtomic API", ex);
        }
        catch (JsonException ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Invalid JSON response from Playtomic API", ex);
        }
        catch (Exception ex)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), $"Unexpected error processing Playtomic API response for club", ex);
        }

    }
    
    private bool IsValidTimeSlot(TimeSlot slot, DateTime date, TimeZoneInfo timeZone)
    {
        var (startDateTime, _) = ParseSlotTimes(slot, date);
    
        if (!startDateTime.HasValue)
        {
            return false;
        }
        
        var timeInPadelClubTimeZone = TimeZoneInfo.ConvertTimeFromUtc(startDateTime.Value, timeZone);
        var hour = timeInPadelClubTimeZone.Hour;
        
        return hour >= _earliestPossibleBookingHour && hour <= _latestPossibleBookingHour;
    }

    private (DateTime? startDateTime, DateTime? endDateTime) ParseSlotTimes(TimeSlot slot, DateTime date)
    {
        if (!TimeSpan.TryParse(slot.StartTime, out var startTimeSpan))
        {
            return (null, null);
        }
        
        var utcStartDateTime = DateTime.SpecifyKind(date.Date.Add(startTimeSpan), DateTimeKind.Utc);
        var utcEndDateTime = utcStartDateTime.AddMinutes(slot.Duration);
        
        return (utcStartDateTime, utcEndDateTime);
    }

    private static string BuildAvailabilityUrl(string baseUrl, string clubId, string date) => $"{baseUrl}/api/clubs/availability?tenant_id={clubId}&date={date}&sport_id=PADEL";

    private static string GetUrlSuffix(string clubName) => clubName.ToLowerInvariant().Replace(" ", "-");

    private static (decimal, string) GetPriceAndCurrency(string price)
    {
        if (string.IsNullOrEmpty(price))
        {
            return (0, string.Empty);
        }
        
        var priceParts = price.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (priceParts.Length < 2 || !decimal.TryParse(priceParts[0], NumberStyles.Currency, CultureInfo.InvariantCulture, out var priceValue))
        {
            return (0, string.Empty);
        }

        var currency = priceParts[1];
        return (priceValue, currency);
    }
}
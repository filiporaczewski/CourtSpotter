using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Core.Results;

namespace PadelCourts.Infrastructure.BookingProviders.Playtomic;

public class PlaytomicBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeZoneInfo _localTimeZone;
    private readonly TimeZoneInfo _apiTimeZone;

    public PlaytomicBookingProvider(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
    {
        _httpClient = httpClientFactory.CreateClient("PlaytomicClient");
        _serviceProvider = serviceProvider;
        _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        _apiTimeZone = TimeZoneInfo.Utc; // Assuming Playtomic API uses UTC

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
        var dailyAvailabilitiesBookingEndpointUrl = $"https://playtomic.com/api/clubs/availability?tenant_id={padelClub.ClubId}&date={dateString}&sport_id=PADEL";

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
                
                var filteredSlots = tenantAvailability.Slots.Where(slot => IsValidTimeSlot(slot, date)).ToList();

                foreach (var slot in filteredSlots)
                {
                    var (startDateTime, endDateTime) = ConvertSlotToLocalTime(slot, date);
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
    
    private bool IsValidTimeSlot(TimeSlot slot, DateTime date)
    {
        var (startDateTime, _) = ConvertSlotToLocalTime(slot, date);
    
        if (!startDateTime.HasValue)
        {
            return false;
        }
    
        var hour = startDateTime.Value.Hour;
        return !(hour is >= 23 or < 6);
    }


    private (DateTime? startDateTime, DateTime? endDateTime) ConvertSlotToLocalTime(TimeSlot slot, DateTime date)
    {
        if (!TimeSpan.TryParse(slot.StartTime, out var startTimeSpan))
        {
            return (null, null);
        }
        
        var apiStartDateTime = DateTime.SpecifyKind(date.Date.Add(startTimeSpan), DateTimeKind.Utc);
        var localStartDateTime = TimeZoneInfo.ConvertTime(apiStartDateTime, _apiTimeZone, _localTimeZone);
        var localEndDateTime = localStartDateTime.AddMinutes(slot.Duration);
        return (localStartDateTime, localEndDateTime);
    }

    
    private string GetUrlSuffix(string clubName)
    {
        return clubName.ToLowerInvariant().Replace(" ", "-");
    }

    private (decimal, string) GetPriceAndCurrency(string price)
    {
        var priceParts = price.Split(' ');
        var priceValue = decimal.Parse(priceParts[0]);
        var currency = priceParts[1];
        return (priceValue, currency);
    }
}
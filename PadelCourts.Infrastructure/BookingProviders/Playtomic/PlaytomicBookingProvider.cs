using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

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
    
    public async Task<IEnumerable<CourtAvailability>> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allAvailabilities = new List<CourtAvailability>();
        using var scope = _serviceProvider.CreateScope();
        var playtomicCourtsRepository = scope.ServiceProvider.GetRequiredService<IPlaytomicCourtsRepository>();
        var bookableCourts = await playtomicCourtsRepository.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken);
        
        var dateTasks = new List<Task<IEnumerable<CourtAvailability>>>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dateTasks.Add(GetCourtAvailabilitiesSingleDay(padelClub, date, bookableCourts, cancellationToken));
        }
        
        var dailyAvailabilitiesResults = await Task.WhenAll(dateTasks);

        foreach (var dailyAvailabilities in dailyAvailabilitiesResults)
        {
            allAvailabilities.AddRange(dailyAvailabilities);
        }

        return allAvailabilities;
    }

    private async Task<IEnumerable<CourtAvailability>> GetCourtAvailabilitiesSingleDay(PadelClub padelClub,
        DateTime date, IEnumerable<PlaytomicCourt> bookableCourts, CancellationToken cancellationToken = default)
    {
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dailyAvailabilitiesBookingEndpointUrl = $"https://playtomic.com/api/clubs/availability?tenant_id={padelClub.ClubId}&date={dateString}&sport_id=PADEL";

        try
        { 
            var dailyAvailabilitiesBookingEndpointResponse = await _httpClient.GetStringAsync(dailyAvailabilitiesBookingEndpointUrl, cancellationToken);
            var tenantAvailabilities = JsonSerializer.Deserialize<List<TenantAvailability>>(dailyAvailabilitiesBookingEndpointResponse);
            var courtAvailabilities = new List<CourtAvailability>();

            if (tenantAvailabilities is null)
            {
                return new List<CourtAvailability>();
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

            return courtAvailabilities;
        } catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Playtomic availability: {ex.Message}");
            return new List<CourtAvailability>();
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
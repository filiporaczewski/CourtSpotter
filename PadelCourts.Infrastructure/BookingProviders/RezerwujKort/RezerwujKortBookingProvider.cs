using System.Globalization;
using System.Text.Json;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders.RezerwujKort;

public class RezerwujKortBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _httpClient;
    
    public RezerwujKortBookingProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("RezerwujKortClient");
    }
    
    public async Task<IEnumerable<CourtAvailability>> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var availabilities = new List<CourtAvailability>();
        var dateTasks = new List<Task<IEnumerable<CourtAvailability>>>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dateTasks.Add(GetDailyAvailabilities(padelClub, date, cancellationToken));
        }
        
        var dailyAvailabilitiesResults = await Task.WhenAll(dateTasks);

        foreach (var dailyAvailabilities in dailyAvailabilitiesResults)
        {
            availabilities.AddRange(dailyAvailabilities);
        }

        return availabilities;
    }

    private async Task<IEnumerable<CourtAvailability>> GetDailyAvailabilities(PadelClub padelClub, DateTime date,
        CancellationToken cancellationToken = default)
    {
        var availabilities = new List<CourtAvailability>();
        
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var apiUrl = $"https://www.rezerwujkort.pl/rest/reservation/one_day_client_reservation_calendar/{GetUrlSuffix(padelClub.Name)}/{dateString}/1/2";
        
        try
        {
            var response = await _httpClient.GetStringAsync(apiUrl, cancellationToken);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Courts == null)
            {
                return availabilities;
            }
        
            foreach (var court in apiResponse.Courts.Where(c => c.OnlineReservation))
            {
                var padelHours = court.Hours.Where(h => h.HourStatus == "OPEN");
        
                foreach (var hour in padelHours)
                {
                    if (!TimeSpan.TryParse(hour.HourName, out var timeSpan))
                    {
                        continue;
                    }
        
                    if (timeSpan.Hours is > 22 or < 6)
                    { 
                        continue;
                    }
                    var startDateTime = date.Add(timeSpan);
                    var now = DateTime.Now;
        
                    if (startDateTime < now)
                    {
                        continue;
                    }
        
                    foreach (var slot in hour.PossibleHalfHourSlots)
                    {
                        var durationInMinutes = slot * 30;
        
                        if (durationInMinutes > 120)
                        {
                            continue;
                        }
                        
                        var endDateTime = startDateTime.AddMinutes(durationInMinutes);
                        
                        availabilities.Add(new CourtAvailability
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClubId = padelClub.ClubId,
                            ClubName = padelClub.Name,
                            CourtName = court.CourtName,
                            StartTime = startDateTime,
                            EndTime = endDateTime,
                            Provider = ProviderType.RezerwujKort,
                            BookingUrl = $"rezerwujkort.pl/klub/{padelClub.Name}/rezerwacja_online?day={dateString}&court={court.CourtId}&hour={hour.HourName}",
                            Type = IsOutdoor(court.CourtDescription) ? CourtType.Outdoor : CourtType.Indoor
                        });
                    }
                }
            }   
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching RezerwujKort availability: {e.Message}");
        }
        
        return availabilities;
    }
    
    private string GetUrlSuffix(string clubName)
    {
        return clubName.ToLowerInvariant().Replace(" ", "_");
    }

    private bool IsOutdoor(string courtDescription)
    {
        return courtDescription.ToLowerInvariant().Contains("odkryt");
    }
}


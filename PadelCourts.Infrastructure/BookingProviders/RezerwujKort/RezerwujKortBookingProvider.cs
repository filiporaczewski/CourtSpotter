using System.Globalization;
using System.Text.Json;
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;
using PadelCourts.Core.Results;

namespace PadelCourts.Infrastructure.BookingProviders.RezerwujKort;

public class RezerwujKortBookingProvider : ICourtBookingProvider
{
    private readonly HttpClient _httpClient;
    private readonly CaseInsensitiveJsonSerializerOptions _serializerOptions;
    private const string BookableHourStatus = "OPEN";
    
    public RezerwujKortBookingProvider(IHttpClientFactory httpClientFactory, CaseInsensitiveJsonSerializerOptions caseInsensitiveJsonSerializerOptions)
    {
        _httpClient = httpClientFactory.CreateClient("RezerwujKortClient") ?? throw new InvalidOperationException("HttpClient 'RezerwujKortClient' is not configured");
        _serializerOptions = caseInsensitiveJsonSerializerOptions ?? throw new ArgumentNullException(nameof(caseInsensitiveJsonSerializerOptions));
    }
    
    public async Task<CourtBookingAvailabilitiesSyncResult> GetCourtBookingAvailabilitiesAsync(PadelClub padelClub, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var availabilities = new List<CourtAvailability>();
        var dateTasks = new List<Task<DailyCourtBookingAvailabilitiesSyncResult>>();
        var failedDailyCourtBookingAvailabilitiesSyncResults = new List<FailedDailyCourtBookingAvailabilitiesSyncResult>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            dateTasks.Add(GetDailyAvailabilities(padelClub, date, cancellationToken));
        }
        
        var dailyAvailabilitiesResults = await Task.WhenAll(dateTasks);

        foreach (var dailySyncResult in dailyAvailabilitiesResults)
        {
            if (dailySyncResult.Success)
            {
                availabilities.AddRange(dailySyncResult.CourtAvailabilities);
            }
            else
            {
                failedDailyCourtBookingAvailabilitiesSyncResults.Add(new FailedDailyCourtBookingAvailabilitiesSyncResult
                {
                    Date = dailySyncResult.Date,
                    Reason = dailySyncResult.FailureReason!
                });
            }
        }

        return new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = availabilities,
            FailedDailyCourtBookingAvailabilitiesSyncResults = failedDailyCourtBookingAvailabilitiesSyncResults
        };   
    }

    private async Task<DailyCourtBookingAvailabilitiesSyncResult> GetDailyAvailabilities(PadelClub padelClub, DateTime date,
        CancellationToken cancellationToken = default)
    {
        var availabilities = new List<CourtAvailability>();
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dailyCourtBookingAvailabilitiesEndpoint = $"https://www.rezerwujkort.pl/rest/reservation/one_day_client_reservation_calendar/{GetUrlSuffix(padelClub.Name)}/{dateString}/1/2";

        try
        {
            var dailyCourtBookingAvailabilitiesEndpointRawResponse =
                await _httpClient.GetStringAsync(dailyCourtBookingAvailabilitiesEndpoint, cancellationToken);
            var dailyCourtBookingAvailabilitiesEndpointJsonResponse =
                JsonSerializer.Deserialize<DailyCourtBookingAvailabilitiesEndpointApiResponse>(
                    dailyCourtBookingAvailabilitiesEndpointRawResponse, _serializerOptions.Value);

            if (dailyCourtBookingAvailabilitiesEndpointJsonResponse?.Courts == null)
            {
                return new DailyCourtBookingAvailabilitiesSyncResult
                {
                    CourtAvailabilities = availabilities,
                    Date = DateOnly.FromDateTime(date),
                    Success = false,
                    FailureReason = "No courts found"
                };
            }

            foreach (var court in dailyCourtBookingAvailabilitiesEndpointJsonResponse.Courts.Where(c =>
                         c.OnlineReservation))
            {
                var bookableHours = court.Hours.Where(h => h.HourStatus == BookableHourStatus);

                foreach (var bookableHour in bookableHours)
                {
                    if (!TimeSpan.TryParse(bookableHour.HourName, out var bookableHourTimeSpan))
                    {
                        continue;
                    }

                    if (bookableHourTimeSpan.Hours is > 22 or < 6)
                    {
                        continue;
                    }

                    var bookingAvailabilityStartDateTime = date.Add(bookableHourTimeSpan);
                    var currentDate = DateTime.Now;

                    if (bookingAvailabilityStartDateTime < currentDate)
                    {
                        continue;
                    }

                    foreach (var possibleHalfHourSlot in bookableHour.PossibleHalfHourSlots)
                    {
                        var availabilityDurationInMinutes = possibleHalfHourSlot * 30;

                        if (availabilityDurationInMinutes > 120)
                        {
                            continue;
                        }

                        var bookingAvailabilityEndDateTime =
                            bookingAvailabilityStartDateTime.AddMinutes(availabilityDurationInMinutes);

                        availabilities.Add(new CourtAvailability
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClubId = padelClub.ClubId,
                            ClubName = padelClub.Name,
                            CourtName = court.CourtName,
                            StartTime = bookingAvailabilityStartDateTime,
                            EndTime = bookingAvailabilityEndDateTime,
                            Provider = ProviderType.RezerwujKort,
                            BookingUrl =
                                $"rezerwujkort.pl/klub/{padelClub.Name}/rezerwacja_online?day={dateString}&court={court.CourtId}&hour={bookableHour.HourName}",
                            Type = IsOutdoor(court.CourtDescription) ? CourtType.Outdoor : CourtType.Indoor
                        });
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Network error calling RezerwujKort API", e);
        }
        catch (TaskCanceledException e)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Timeout when calling RezerwujKort API", e);
        }
        catch (JsonException e)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Invalid JSON response from RezerwujKort API", e);
        }
        catch (Exception e)
        {
            return DailyCourtBookingAvailabilitiesSyncResult.CreateFailedResult(DateOnly.FromDateTime(date), "Unexpected error when syncing court availabilities from RezerwujKort provider", e);
        }

        return new DailyCourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = availabilities,
            Date = DateOnly.FromDateTime(date),
            Success = true
        };
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


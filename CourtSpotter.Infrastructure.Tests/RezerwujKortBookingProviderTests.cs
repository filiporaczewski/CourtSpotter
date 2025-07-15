using System.Net;
using System.Text.Json;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Options;
using CourtSpotter.Infrastructure.BookingProviders.RezerwujKort;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;
using Shouldly;

namespace CourtSpotter.Infrastructure.Tests;

public class RezerwujKortBookingProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly CaseInsensitiveJsonSerializerOptions _serializerOptions;
    private readonly RezerwujKortBookingProvider _provider;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly CourtBookingAvailabilitiesSyncOptions _syncOptions;

    private const string HttpClientName = "RezerwujKortClient";
    
    public RezerwujKortBookingProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(HttpClientName)).Returns(_httpClient);
        _serializerOptions = new CaseInsensitiveJsonSerializerOptions();
        _fakeTimeProvider = new FakeTimeProvider();
        
        _syncOptions = new CourtBookingAvailabilitiesSyncOptions
        {
            DaysToSyncCount = 2,
            EarliestBookingHour = 6,
            LatestBookingHour = 22,
            UpdatePeriod = 10
        };
        
        var courtBookingAvailabilitiesSyncOptionsMock = new Mock<IOptions<CourtBookingAvailabilitiesSyncOptions>>();
        courtBookingAvailabilitiesSyncOptionsMock.Setup(o => o.Value).Returns(_syncOptions);
        
        _provider = new RezerwujKortBookingProvider(_httpClientFactoryMock.Object, _serializerOptions, _fakeTimeProvider, courtBookingAvailabilitiesSyncOptionsMock.Object);;
    }
    
    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenHttpRequestExceptionOccurs_ReturnsFailedResultWithNetworkErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test club"
        };
        
        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        
        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);
        
        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Network error calling RezerwujKort API");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenTimeoutOccurs_ReturnsFailedResultWithTimeoutErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));
        
        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);
        
        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Timeout when calling RezerwujKort API");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenJsonDeserializationFails_ReturnsFailedResultWithJsonErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        var invalidJsonResponse = "{ invalid json content }";
        
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
        
        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);
        
        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Invalid JSON response from RezerwujKort API");
    }
    
    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenUnexpectedExceptionOccurs_ReturnsFailedResultWithUnexpectedErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error occurred"));

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Unexpected error when syncing court availabilities from RezerwujKort provider");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenValidApiResponse_ReturnsCorrectSyncResultWithFilteredAvailabilities()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var currentTime = new DateTime(2025, 7, 10, 10, 30, 0); // Saturday, 10:30 AM
        _fakeTimeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        var startDate = currentTime.Date.AddDays(1); // Tomorrow
        var endDate = currentTime.Date.AddDays(3);   // 3 days from now
        var cancellationToken = CancellationToken.None;

        SetupValidApiResponse(startDate, padelClub);
        SetupValidApiResponse(startDate.AddDays(1), padelClub);
        SetupValidApiResponse(startDate.AddDays(2), padelClub);
        
        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldBeEmpty();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubId == padelClub.ClubId);
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubName == padelClub.Name);
        result.CourtAvailabilities.ShouldAllBe(a => a.Provider == ProviderType.RezerwujKort);
        
        // Verify time filtering - no availabilities before current time
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime >= currentTime);
        
        // Verify hour filtering - no availabilities before 6am or after 10pm (22:00)
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Hour >= 6);
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Hour <= 22);
        
        // Verify court types based on description
        var indoorCourts = result.CourtAvailabilities.Where(a => a.CourtName == "Indoor Court 1");
        var outdoorCourts = result.CourtAvailabilities.Where(a => a.CourtName == "Outdoor Court odkryty");

        indoorCourts.ShouldAllBe(a => a.Type == CourtType.Indoor);
        outdoorCourts.ShouldAllBe(a => a.Type == CourtType.Outdoor);

        // Verify duration variations (60 minutes, 90 minutes, 120 minutes)
        var durations = result.CourtAvailabilities.Select(a => a.Duration.TotalMinutes);
        
        durations.ShouldNotContain(30);
        durations.ShouldContain(60);
        durations.ShouldContain(90);
        durations.ShouldContain(120);

        // Verify booking URLs are correctly formatted
        result.CourtAvailabilities.ShouldAllBe(a => a.BookingUrl.StartsWith("https://www.rezerwujkort.pl/klub/") && a.BookingUrl.Contains("rezerwacja_online"));;
        
        // Verify that only courts with OnlineReservation = true are included
        result.CourtAvailabilities.ShouldNotContain(a => a.CourtName == "Offline Court");
    }
    
    private void SetupValidApiResponse(DateTime date, PadelClub padelClub)
    {
       var dateString = date.ToString("yyyy-MM-dd");
        var clubNameForUrl = padelClub.Name.ToLowerInvariant().Replace(" ", "_");
        var expectedUrl = $"https://www.rezerwujkort.pl/rest/reservation/one_day_client_reservation_calendar/{clubNameForUrl}/{dateString}/1/2";

        var apiResponse = new DailyCourtBookingAvailabilitiesEndpointApiResponse
        {
            Date = dateString,
            Courts =
            [
                new Court
                {
                    CourtId = 1,
                    CourtName = "Indoor Court 1",
                    CourtDescription = "Indoor court with standard equipment",
                    OnlineReservation = true,
                    Hours =
                    [
                        new Hour
                        {
                            HourId = 1,
                            HourName = "05:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3, 4]
                        },
                        // Valid morning hour with all valid durations
                        new Hour
                        {
                            HourId = 2,
                            HourName = "08:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3, 4] // 60min, 90min, 120min
                        },
                        // Valid afternoon hour
                        new Hour
                        {
                            HourId = 3,
                            HourName = "14:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3, 4] // 60min, 90min, 120min
                        },
                        // Valid evening hour
                        new Hour
                        {
                            HourId = 4,
                            HourName = "20:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3] // 60min, 90min
                        },
                        // Hour after 10pm - should be filtered out
                        new Hour
                        {
                            HourId = 5,
                            HourName = "23:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3]
                        },
                        // Closed hour - should be filtered out
                        new Hour
                        {
                            HourId = 6,
                            HourName = "10:00",
                            HourStatus = "CLOSED",
                            PossibleHalfHourSlots = [2, 3]
                        }
                    ]
                },

                new Court
                {
                    CourtId = 2,
                    CourtName = "Outdoor Court odkryty",
                    CourtDescription = "Outdoor court odkryty with natural lighting",
                    OnlineReservation = true,
                    Hours =
                    [
                        new Hour
                        {
                            HourId = 7,
                            HourName = "09:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 4] // 60min, 120min
                        },
                        new Hour
                        {
                            HourId = 8,
                            HourName = "16:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3] // 60min, 90min
                        }
                    ]
                },
                // Court with OnlineReservation = false - should be filtered out
                new Court
                {
                    CourtId = 3,
                    CourtName = "Offline Court",
                    CourtDescription = "Court without online reservation",
                    OnlineReservation = false,
                    Hours =
                    [
                        new Hour
                        {
                            HourId = 9,
                            HourName = "12:00",
                            HourStatus = "OPEN",
                            PossibleHalfHourSlots = [2, 3]
                        }
                    ]
                }
            ]
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse, _serializerOptions.Value);
        
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }
}
using System.Net;
using System.Text.Json;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Options;
using CourtSpotter.Infrastructure.BookingProviders.Playtomic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Shouldly;

namespace CourtSpotter.Infrastructure.Tests;

public class PlaytomicBookingProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _scopeServiceProviderMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IPlaytomicCourtsRepository> _playtomicCourtsRepositoryMock;
    private readonly HttpClient _httpClient;
    private readonly PlaytomicProviderOptions _playtomicOptions;
    private readonly PlaytomicBookingProvider _provider;
    private readonly CourtBookingAvailabilitiesSyncOptions _syncOptions;

    private const string HttpClientName = "PlaytomicClient";

    public PlaytomicBookingProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeServiceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _playtomicCourtsRepositoryMock = new Mock<IPlaytomicCourtsRepository>();
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(HttpClientName)).Returns(_httpClient);
        
        _playtomicOptions = new PlaytomicProviderOptions
        {
            ApiBaseUrl = "https://playtomic.com",
            LocalTimeZoneId = "UTC",
            ApiTimeZoneId = "UTC"
        };
        
        var playtomicOptionsMock = new Mock<IOptions<PlaytomicProviderOptions>>();
        playtomicOptionsMock.Setup(o => o.Value).Returns(_playtomicOptions);
        
        _syncOptions = new CourtBookingAvailabilitiesSyncOptions
        {
            DaysToSyncCount = 2,
            EarliestBookingHour = 6,
            LatestBookingHour = 22,
            UpdatePeriod = 10
        };
        
        var courtBookingAvailabilitiesSyncOptionsMock = new Mock<IOptions<CourtBookingAvailabilitiesSyncOptions>>();
        courtBookingAvailabilitiesSyncOptionsMock.Setup(o => o.Value).Returns(_syncOptions);
        
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_scopeServiceProviderMock.Object);
        _scopeServiceProviderMock.Setup(sp => sp.GetService(typeof(IPlaytomicCourtsRepository))).Returns(_playtomicCourtsRepositoryMock.Object);
        
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
        
        _provider = new PlaytomicBookingProvider(_httpClientFactoryMock.Object, _serviceProviderMock.Object, playtomicOptionsMock.Object, courtBookingAvailabilitiesSyncOptionsMock.Object);;
    }
    
    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenHttpRequestExceptionOccurs_ReturnsFailedResultWithNetworkErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };
        
        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());
        
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
        failedDateResult.Reason.ShouldBe("Network error calling Playtomic API");
    }
    
    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenTimeoutOccurs_ReturnsFailedResultWithTimeoutErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());
        
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
        failedDateResult.Reason.ShouldBe("Request timeout calling Playtomic API");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenJsonDeserializationFails_ReturnsFailedResultWithJsonErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());
        
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
        failedDateResult.Reason.ShouldBe("Invalid JSON response from Playtomic API");
    }
    
    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenUnexpectedExceptionOccurs_ReturnsFailedResultWithUnexpectedErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());

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
        failedDateResult.Reason.ShouldBe("Unexpected error processing Playtomic API response for club");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenEmptyApiResponse_ReturnsFailedResultWithEmptyResponseMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());
        
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
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
        failedDateResult.Reason.ShouldBe("Empty response from Playtomic API");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenNullDeserializationResult_ReturnsFailedResultWithDeserializationErrorMessage()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;
        
        _playtomicCourtsRepositoryMock
            .Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken))
            .ReturnsAsync(new List<PlaytomicCourt>());
        
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
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
        failedDateResult.Reason.ShouldBe("Failed to deserialize response from Playtomic API");
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

        var startDate = DateTime.Today.AddDays(1);
        var endDate = DateTime.Today.AddDays(3);
        var cancellationToken = CancellationToken.None;
        
        List<PlaytomicCourt> playtomicCourts =
        [
            new()
            {
                Id = "court-1",
                Name = "Indoor Court 1",
                Type = CourtType.Indoor,
                ClubId = padelClub.ClubId
            },
            new()
            {
                Id = "court-2",
                Name = "Outdoor Court 2",
                Type = CourtType.Outdoor,
                ClubId = padelClub.ClubId
            }
        ];
        
        _playtomicCourtsRepositoryMock.Setup(r => r.GetPlaytomicCourtsByClubId(padelClub.ClubId, cancellationToken)).ReturnsAsync(playtomicCourts);

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
        result.CourtAvailabilities.ShouldAllBe(a => a.Provider == ProviderType.Playtomic);

        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Hour >= 6);
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Hour <= 22);
        
        var indoorCourts = result.CourtAvailabilities.Where(a => a.CourtName == "Indoor Court 1");
        var outdoorCourts = result.CourtAvailabilities.Where(a => a.CourtName == "Outdoor Court 2");
        
        indoorCourts.ShouldAllBe(a => a.Type == CourtType.Indoor);
        outdoorCourts.ShouldAllBe(a => a.Type == CourtType.Outdoor);

        result.CourtAvailabilities.ShouldAllBe(a => a.BookingUrl.StartsWith("https://playtomic.com/clubs/") && a.BookingUrl.Contains("test-club"));
        result.CourtAvailabilities.ShouldAllBe(a => a.Price > 0);
        result.CourtAvailabilities.ShouldAllBe(a => !string.IsNullOrEmpty(a.Currency));
        
        result.CourtAvailabilities.ShouldAllBe(a => a.Duration.TotalMinutes == 90);
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime != default);
        result.CourtAvailabilities.ShouldAllBe(a => a.EndTime != default);
        result.CourtAvailabilities.ShouldAllBe(a => a.EndTime > a.StartTime);
    }
    
    private void SetupValidApiResponse(DateTime date, PadelClub padelClub)
    {
        var dateString = date.ToString("yyyy-MM-dd");
        var expectedUrl = $"https://playtomic.com/api/clubs/availability?tenant_id={padelClub.ClubId}&date={dateString}&sport_id=PADEL";

        List<TenantAvailability> apiResponse = [
            new()
            {
                ResourceId = "court-1",
                StartDate = dateString,
                Slots =
                [
                    new TimeSlot
                    {
                        StartTime = "08:00",
                        Duration = 90,
                        Price = "25.50 EUR"
                    },
                    // Valid afternoon slot  
                    new TimeSlot
                    {
                        StartTime = "14:00",
                        Duration = 90,
                        Price = "30.00 EUR"
                    },
                    // Valid evening slot
                    new TimeSlot
                    {
                        StartTime = "20:00",
                        Duration = 90,
                        Price = "35.00 EUR"
                    },
                    // Invalid slot - too early (should be filtered out)
                    new TimeSlot
                    {
                        StartTime = "05:00",
                        Duration = 90,
                        Price = "20.00 EUR"
                    },
                    // Invalid slot - too late (should be filtered out)
                    new TimeSlot
                    {
                        StartTime = "23:00",
                        Duration = 90,
                        Price = "40.00 EUR"
                    }
                ]
            },
            new()
            {
                ResourceId = "court-2",
                StartDate = dateString,
                Slots =
                [
                    new TimeSlot
                    {
                        StartTime = "09:00",
                        Duration = 90,
                        Price = "28.00 EUR"
                    },
                    // Valid afternoon slot
                    new TimeSlot
                    {
                        StartTime = "16:00",
                        Duration = 90,
                        Price = "32.00 EUR"
                    }
                ]
            },
            // Availability for court not in repository (should be filtered out)

            new()
            {
                ResourceId = "unknown-court",
                StartDate = dateString,
                Slots =
                [
                    new TimeSlot
                    {
                        StartTime = "12:00",
                        Duration = 90,
                        Price = "25.00 EUR"
                    }
                ]
            }
        ];

        var jsonResponse = JsonSerializer.Serialize(apiResponse);
        
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
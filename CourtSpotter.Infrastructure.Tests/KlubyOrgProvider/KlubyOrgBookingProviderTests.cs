using System.Net;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using Moq;
using Moq.Protected;
using Shouldly;

namespace CourtSpotter.Infrastructure.Tests.KlubyOrgProvider;

public class KlubyOrgBookingProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IKlubyOrgAuthenticationService> _authenticationServiceMock;
    private readonly Mock<IKlubyOrgScheduleParser> _scheduleParserMock;
    private readonly KlubyOrgCourtBookingProvider _provider;

    private const string HttpClientName = "KlubyOrgClient";
    private const string TestBaseUrl = "https://kluby.org/";

    public KlubyOrgBookingProviderTests()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _authenticationServiceMock = new Mock<IKlubyOrgAuthenticationService>();
        _scheduleParserMock = new Mock<IKlubyOrgScheduleParser>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(TestBaseUrl)
        };
        
        httpClientFactoryMock.Setup(f => f.CreateClient(HttpClientName)).Returns(httpClient);
        
        _provider = new KlubyOrgCourtBookingProvider(
            httpClientFactoryMock.Object,
            _authenticationServiceMock.Object,
            _scheduleParserMock.Object);
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenAuthenticationFails_ReturnsFailedResultWithAuthenticationError()
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Failed to authenticate to kluby.org");
        failedDateResult.Exception.ShouldBeNull();
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenAuthenticationThrowsException_ReturnsFailedResultWithException()
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

        var authException = new InvalidOperationException("Authentication service error");
        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ThrowsAsync(authException);

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Error authenticating to kluby.org");
        failedDateResult.Exception.ShouldBe(authException);
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

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
        failedDateResult.Reason.ShouldBe("Network error calling KlubyOrg");
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

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
        failedDateResult.Reason.ShouldBe("Request timeout calling KlubyOrg");
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

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
        failedDateResult.Reason.ShouldBe("Unexpected error processing KlubyOrg");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenEmptyResponse_ReturnsFailedResultWithEmptyResponseMessage()
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

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
        failedDateResult.Reason.ShouldBe("Empty response from KlubyOrg");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenScheduleParserFails_ReturnsFailedResultWithParsingError()
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

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

        SetupValidHttpResponse();

        _scheduleParserMock
            .Setup(p => p.ParseScheduleAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<PadelClub>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KlubyOrgScheduleParserResult(
                new List<CourtAvailability>(),
                false,
                "Failed to parse schedule"));

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.CourtAvailabilities.ShouldBeEmpty();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        
        var failedDateResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedDateResult.Date.ShouldBe(DateOnly.FromDateTime(startDate));
        failedDateResult.Reason.ShouldBe("Failed to parse schedule");
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenValidResponse_ReturnsCorrectSyncResultWithAvailabilities()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var cancellationToken = CancellationToken.None;

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

        SetupValidHttpResponseForMultipleRequests();

        var expectedAvailabilities = new List<CourtAvailability>
        {
            new()
            {
                Id = "1",
                ClubId = padelClub.ClubId,
                ClubName = padelClub.Name,
                CourtName = "Court 1",
                Provider = ProviderType.KlubyOrg,
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10),
                Type = CourtType.Indoor,
                BookingUrl = "https://kluby.org/test-club/grafik",
                Currency = "PLN",
                Price = 0
            }
        };

        _scheduleParserMock
            .Setup(p => p.ParseScheduleAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<PadelClub>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KlubyOrgScheduleParserResult(expectedAvailabilities, true, null));

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldBeEmpty();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        // Should have 3 days worth of availabilities (today + 2 days)
        result.CourtAvailabilities.Count.ShouldBe(3);
        
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubId == padelClub.ClubId);
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubName == padelClub.Name);
        result.CourtAvailabilities.ShouldAllBe(a => a.Provider == ProviderType.KlubyOrg);
        
        // Verify parser was called for each date
        _scheduleParserMock.Verify(p => p.ParseScheduleAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<PadelClub>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenClubHasMultiplePages_ProcessesAllPages()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club",
            PagesCount = 2
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var cancellationToken = CancellationToken.None;

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

        SetupValidHttpResponseForMultipleRequests();

        var expectedAvailabilities = new List<CourtAvailability>
        {
            new()
            {
                Id = "1",
                ClubId = padelClub.ClubId,
                ClubName = padelClub.Name,
                CourtName = "Court 1",
                Provider = ProviderType.KlubyOrg,
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10),
                Type = CourtType.Indoor,
                BookingUrl = "https://kluby.org/test-club/grafik",
                Currency = "PLN",
                Price = 0
            }
        };

        _scheduleParserMock
            .Setup(p => p.ParseScheduleAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<PadelClub>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KlubyOrgScheduleParserResult(expectedAvailabilities, true, null));

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldBeEmpty();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        // Should have 2 pages worth of availabilities (2 pages for 1 day)
        result.CourtAvailabilities.Count.ShouldBe(2);
        
        // Verify parser was called for each page
        _scheduleParserMock.Verify(p => p.ParseScheduleAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<PadelClub>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        
        // Verify correct URLs were called for each page
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.RequestUri != null && 
                req.RequestUri.ToString().Contains("test-club/grafik?data_grafiku=") &&
                req.RequestUri.ToString().Contains("strona=0")),
            ItExpr.IsAny<CancellationToken>());
            
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.RequestUri != null && 
                req.RequestUri.ToString().Contains("test-club/grafik?data_grafiku=") &&
                req.RequestUri.ToString().Contains("strona=1")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetCourtBookingAvailabilitiesAsync_WhenMixedSuccessAndFailures_ReturnsPartialResults()
    {
        // Arrange
        var padelClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club"
        };

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var cancellationToken = CancellationToken.None;

        _authenticationServiceMock
            .Setup(a => a.EnsureAuthenticatedAsync(cancellationToken))
            .ReturnsAsync(true);

        // Setup first day to succeed
        var httpResponseMessage1 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Valid HTML</html>")
        };
        
        // Setup second day to fail with network error
        // Setup third day to succeed
        var httpResponseMessage3 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Valid HTML</html>")
        };

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage1)
            .ThrowsAsync(new HttpRequestException("Network error"))
            .ReturnsAsync(httpResponseMessage3);

        var successAvailabilities = new List<CourtAvailability>
        {
            new()
            {
                Id = "1",
                ClubId = padelClub.ClubId,
                ClubName = padelClub.Name,
                CourtName = "Court 1",
                Provider = ProviderType.KlubyOrg,
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10),
                Type = CourtType.Indoor,
                BookingUrl = "https://kluby.org/test-club/grafik",
                Currency = "PLN",
                Price = 0
            }
        };

        _scheduleParserMock
            .Setup(p => p.ParseScheduleAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<PadelClub>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KlubyOrgScheduleParserResult(successAvailabilities, true, null));

        // Act
        var result = await _provider.GetCourtBookingAvailabilitiesAsync(padelClub, startDate, endDate, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        
        // Should have 2 successful days
        result.CourtAvailabilities.Count.ShouldBe(2);
        
        // Should have 1 failed day
        result.FailedDailyCourtBookingAvailabilitiesSyncResults.ShouldHaveSingleItem();
        
        var failedResult = result.FailedDailyCourtBookingAvailabilitiesSyncResults.Single();
        failedResult.Date.ShouldBe(DateOnly.FromDateTime(startDate.AddDays(1)));
        failedResult.Reason.ShouldBe("Network error calling KlubyOrg");
    }

    private void SetupValidHttpResponse()
    {
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Valid HTML content</html>")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }
    
    private void SetupValidHttpResponseForMultipleRequests()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html>Valid HTML content</html>")
            });
    }
}

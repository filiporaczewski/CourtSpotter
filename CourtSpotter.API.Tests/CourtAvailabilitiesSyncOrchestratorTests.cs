using CourtSpotter.API.Tests.Extensions;
using CourtSpotter.BackgroundServices.CourtBookingAvailabilitiesSync;
using CourtSpotter.Core.Contracts;
using CourtSpotter.Core.Models;
using CourtSpotter.Core.Options;
using CourtSpotter.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace CourtSpotter.API.Tests;

public class CourtAvailabilitiesSyncOrchestratorTests
{
    private readonly Mock<ILogger<CourtAvailabilitiesSyncOrchestrator>> _loggerMock;
    private readonly Mock<IPadelClubsRepository> _padelClubsRepositoryMock;
    private readonly Mock<ICourtAvailabilityRepository> _courtAvailabilityRepositoryMock;
    private readonly Mock<ICourtBookingProviderResolver> _providerResolverMock;
    private readonly Mock<ICourtBookingProvider> _providerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly CourtAvailabilitiesSyncOrchestrator _orchestrator;

    public CourtAvailabilitiesSyncOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<CourtAvailabilitiesSyncOrchestrator>>();
        _padelClubsRepositoryMock = new Mock<IPadelClubsRepository>();
        _courtAvailabilityRepositoryMock = new Mock<ICourtAvailabilityRepository>();
        _providerResolverMock = new Mock<ICourtBookingProviderResolver>();
        _providerMock = new Mock<ICourtBookingProvider>();
        _timeProvider = new FakeTimeProvider();

        var options = new CourtBookingAvailabilitiesSyncOptions
        {
            DaysToSyncCount = 3
        };

        var optionsMock = new Mock<IOptions<CourtBookingAvailabilitiesSyncOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        _orchestrator = new CourtAvailabilitiesSyncOrchestrator(
            _loggerMock.Object,
            _padelClubsRepositoryMock.Object,
            _courtAvailabilityRepositoryMock.Object,
            _providerResolverMock.Object,
            optionsMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task OrchestrateSyncAsync_WhenSuccessful_CallsProviderForEachClubWithCorrectDateRange()
    {
        var currentTime = new DateTime(2025, 1, 15, 10, 30, 0);
        _timeProvider.SetUtcNow(currentTime);
        
        var expectedStartDate = currentTime.Date;
        var expectedEndDate = currentTime.Date.AddDays(3);
        
        List<PadelClub> clubs =
        [
            new() { ClubId = "club1", Name = "Club 1", Provider = ProviderType.Playtomic },
            new() { ClubId = "club2", Name = "Club 2", Provider = ProviderType.KlubyOrg }
        ];
        
        _padelClubsRepositoryMock.Setup(r => r.GetPadelClubs(It.IsAny<CancellationToken>())).ReturnsAsync(clubs);
        _providerResolverMock.Setup(r => r.GetProvider(It.IsAny<ProviderType>())).Returns(_providerMock.Object);
        
        var syncResult = new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = [],
            FailedDailyCourtBookingAvailabilitiesSyncResults = []
        };
        
        _providerMock.Setup(p => p.GetCourtBookingAvailabilitiesAsync(
                It.IsAny<PadelClub>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        
        _courtAvailabilityRepositoryMock.Setup(r => r.GetAvailabilitiesAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<CourtType?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourtAvailability>());
        
        // Act
        await _orchestrator.OrchestrateSyncAsync();
        
        // Assert
        _providerMock.Verify(p => p.GetCourtBookingAvailabilitiesAsync(
            It.Is<PadelClub>(c => c.ClubId == "club1"),
            expectedStartDate,
            expectedEndDate,
            It.IsAny<CancellationToken>()), Times.Once);

        _providerMock.Verify(p => p.GetCourtBookingAvailabilitiesAsync(
            It.Is<PadelClub>(c => c.ClubId == "club2"),
            expectedStartDate,
            expectedEndDate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrchestrateSyncAsync_WhenAvailabilitiesReturned_SyncsWithCorrectDiff()
    {
        // Arrange
        var currentTime = new DateTime(2025, 1, 15, 10, 30, 0);
        _timeProvider.SetUtcNow(currentTime);

        var clubs = new List<PadelClub>
        {
            new() { ClubId = "club1", Name = "Club 1", Provider = ProviderType.Playtomic }
        };
        
        var newAvailabilities = new List<CourtAvailability>
        {
            new()
            {
                Id = "new1",
                ClubId = "club1",
                StartTime = currentTime.AddHours(1),
                EndTime = currentTime.AddHours(2),
                CourtName = "Court 1"
            }
        };
        
        var existingAvailabilities = new List<CourtAvailability>
        {
            new()
            {
                Id = "existing1",
                ClubId = "club1",
                StartTime = currentTime.AddHours(2),
                EndTime = currentTime.AddHours(3),
                CourtName = "Court 2"
            }
        };
        
        _padelClubsRepositoryMock.Setup(r => r.GetPadelClubs(It.IsAny<CancellationToken>())).ReturnsAsync(clubs);
        _providerResolverMock.Setup(r => r.GetProvider(It.IsAny<ProviderType>())).Returns(_providerMock.Object);
        
        var syncResult = new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = newAvailabilities,
            FailedDailyCourtBookingAvailabilitiesSyncResults = []
        };
        
        _providerMock.Setup(p => p.GetCourtBookingAvailabilitiesAsync(
                It.IsAny<PadelClub>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);
        
        _courtAvailabilityRepositoryMock.Setup(r => r.GetAvailabilitiesAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<CourtType?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAvailabilities);
        
        // Act
        await _orchestrator.OrchestrateSyncAsync();
        
        // Assert
        _courtAvailabilityRepositoryMock.Verify(r => r.SaveAvailabilitiesAsync(
            It.Is<List<CourtAvailability>>(list => list.Count == 1 && list[0].Id == "new1"),
            It.IsAny<CancellationToken>()), Times.Once);

        _courtAvailabilityRepositoryMock.Verify(r => r.DeleteAvailabilitiesAsync(
            It.Is<List<CourtAvailability>>(list => list.Count == 1 && list[0].Id == "existing1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrchestrateSyncAsync_WhenProviderFails_LogsFailuresAndContinuesWithOtherClubs()
    {
        // Arrange
        var currentTime = new DateTime(2025, 1, 15, 10, 30, 0);
        _timeProvider.SetUtcNow(currentTime);

        var clubs = new List<PadelClub>
        {
            new() { ClubId = "club1", Name = "Club 1", Provider = ProviderType.Playtomic },
            new() { ClubId = "club2", Name = "Club 2", Provider = ProviderType.KlubyOrg }
        };

        var failedResult = new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = [],
            FailedDailyCourtBookingAvailabilitiesSyncResults =
            [
                new FailedDailyCourtBookingAvailabilitiesSyncResult
                {
                    Date = DateOnly.FromDateTime(currentTime),
                    Reason = "Network error",
                    Exception = null
                }
            ]
        };
        
        var successResult = new CourtBookingAvailabilitiesSyncResult
        {
            CourtAvailabilities = [
                new CourtAvailability
                {
                    Id = "success1",
                    ClubId = "club2",
                    StartTime = currentTime.AddHours(1),
                    EndTime = currentTime.AddHours(2),
                    CourtName = "Court 1"
                }
            ],
            FailedDailyCourtBookingAvailabilitiesSyncResults = []
        };
        
        _padelClubsRepositoryMock.Setup(r => r.GetPadelClubs(It.IsAny<CancellationToken>()))
            .ReturnsAsync(clubs);

        _providerResolverMock.Setup(r => r.GetProvider(It.IsAny<ProviderType>()))
            .Returns(_providerMock.Object);
        
        _providerMock.Setup(p => p.GetCourtBookingAvailabilitiesAsync(
                It.Is<PadelClub>(c => c.ClubId == "club1"),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        _providerMock.Setup(p => p.GetCourtBookingAvailabilitiesAsync(
                It.Is<PadelClub>(c => c.ClubId == "club2"),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);
        
        _courtAvailabilityRepositoryMock.Setup(r => r.GetAvailabilitiesAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<CourtType?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourtAvailability>());
        
        // Act
        await _orchestrator.OrchestrateSyncAsync();
        
        // Assert
        _loggerMock.VerifyLog(LogLevel.Error, "Network error", Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Successfully synced 1 availabilities for club: Club 2", Times.Once);
        
        _courtAvailabilityRepositoryMock.Verify(r => r.SaveAvailabilitiesAsync(
            It.Is<List<CourtAvailability>>(list => list.Count == 1 && list[0].ClubId == "club2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
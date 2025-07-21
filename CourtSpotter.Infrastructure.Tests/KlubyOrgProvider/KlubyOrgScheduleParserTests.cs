using CourtSpotter.Core.Models;
using CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace CourtSpotter.Infrastructure.Tests.KlubyOrgProvider;

public class KlubyOrgScheduleParserTests
{
    private readonly KlubyOrgProviderOptions _options;
    private readonly KlubyOrgScheduleParser _parser;
    private readonly PadelClub _testClub;
    private readonly DateTime _testDate;
    private readonly string _testScheduleUrl;

    public KlubyOrgScheduleParserTests()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        
        _options = new KlubyOrgProviderOptions
        {
            BaseUrl = "https://kluby.org"
        };

        var optionsMock = new Mock<IOptions<KlubyOrgProviderOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        _parser = new KlubyOrgScheduleParser(fakeTimeProvider, optionsMock.Object);

        _testClub = new PadelClub
        {
            ClubId = "test-club-id",
            Name = "Test Club",
            TimeZone = "Europe/Warsaw"
        };

        _testDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);
        _testScheduleUrl = "test-club/grafik?data_grafiku=2024-01-15";
        
        // Set the fake time provider to simulate the current time as the test date
        // This ensures timezone conversions work correctly
        var centralEuropeanTime = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        var testDateTime = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Unspecified);
        var testDateTimeOffset = new DateTimeOffset(testDateTime, centralEuropeanTime.GetUtcOffset(testDateTime));
        fakeTimeProvider.SetUtcNow(testDateTimeOffset.UtcDateTime);
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenScheduleTableIsMissing_ShouldReturnFailureResult()
    {
        // Arrange
        const string htmlWithoutScheduleTable = """
                                                <html>
                                                    <body>
                                                        <div class="content">
                                                            <h1>Some other content</h1>
                                                            <p>No schedule table here</p>
                                                        </div>
                                                    </body>
                                                </html>
                                                """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithoutScheduleTable, 
            _testDate, 
            _testClub,
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Failed to find booking schedule table");
        result.CourtAvailabilities.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenScheduleTableHasNoRows_ShouldReturnFailureResult()
    {
        // Arrange
        var htmlWithEmptyScheduleTable = """
                                         <html>
                                             <body>
                                                 <div class="content">
                                                     <table id="grafik">
                                                         <thead>
                                                             <tr>
                                                                 <th>Time</th>
                                                                 <th>Court 1</th>
                                                                 <th>Court 2</th>
                                                             </tr>
                                                         </thead>
                                                         <tbody>
                                                             <!-- No rows here -->
                                                         </tbody>
                                                     </table>
                                                 </div>
                                             </body>
                                         </html>
                                         """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithEmptyScheduleTable, 
            _testDate, 
            _testClub, 
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Failed to find any half hour slot rows");
        result.CourtAvailabilities.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenCourtNamesAreClean_ShouldExtractCorrectNames()
    {
        // Arrange
        var htmlWithCleanCourtNames = """
                                      <html>
                                          <body>
                                              <table id="grafik">
                                                  <thead>
                                                      <tr>
                                                          <th>Time</th>
                                                          <th>Court 1</th>
                                                          <th>Court 2 Indoor</th>
                                                          <th>Court 3 Outdoor</th>
                                                      </tr>
                                                  </thead>
                                                  <tbody>
                                                      <tr>
                                                          <td>09:00</td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                      </tr>
                                                      <tr>
                                                          <td>09:30</td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                          <td><a href="rezerwuj">Book</a></td>
                                                      </tr>
                                                  </tbody>
                                              </table>
                                          </body>
                                      </html>
                                      """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithCleanCourtNames, 
            _testDate, 
            _testClub, 
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeTrue();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        var courtNames = result.CourtAvailabilities.Select(a => a.CourtName).Distinct().ToList();
        courtNames.ShouldContain("Court 1");
        courtNames.ShouldContain("Court 2 Indoor");
        courtNames.ShouldContain("Court 3 Outdoor");
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenExtractingCourtNames_ShouldHandleVariousHeaderFormats()
    {
        // Arrange
        const string htmlWithVariousCourtNameFormats = """
                                                       <html>
                                                           <body>
                                                               <table id="grafik">
                                                                   <thead>
                                                                       <tr>
                                                                           <th>Time</th>
                                                                           <th>Court 1</th>
                                                                           <th>	Court 2 hala	
                                                                               Extra info</th>
                                                                           <th>   Court 3   
                                                                           </th>
                                                                           <th>   </th>
                                                                           <th></th>
                                                                       </tr>
                                                                   </thead>
                                                                   <tbody>
                                                                       <tr>
                                                                           <td>09:00</td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                       </tr>
                                                                       <tr>
                                                                           <td>09:30</td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                           <td><a href="rezerwuj">Book</a></td>
                                                                       </tr>
                                                                   </tbody>
                                                               </table>
                                                           </body>
                                                       </html>
                                                       """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithVariousCourtNameFormats, 
            _testDate, 
            _testClub, 
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeTrue();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        var courtNames = result.CourtAvailabilities.Select(a => a.CourtName).Distinct().ToList();
        
        // Clean name should be extracted as-is
        courtNames.ShouldContain("Court 1");
        
        // Messy name should be cleaned (tabs removed, first line extracted)
        courtNames.ShouldContain("Court 2 hala");
        
        // Whitespace should be trimmed
        courtNames.ShouldContain("Court 3");
        
        // Empty/whitespace headers should default to "Unknown Court"
        courtNames.ShouldContain("Unknown Court");
        
        // Should have exactly 2 "Unknown Court" entries (for the 2 empty headers)
        var unknownCourts = result.CourtAvailabilities.Where(a => a.CourtName == "Unknown Court").ToList();
        unknownCourts.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenParsingTimeSlots_ShouldDetectAvailabilityAndGenerateCorrectBookings()
    {
        // Arrange
        const string htmlWithTimeSlots = """
                                         <html>
                                             <body>
                                                 <table id="grafik">
                                                     <thead>
                                                         <tr>
                                                             <th>Time</th>
                                                             <th>Court 1</th>
                                                             <th>Court 2</th>
                                                         </tr>
                                                     </thead>
                                                     <tbody>
                                                         <tr>
                                                             <td>09:00</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                             <td>Occupied</td>
                                                         </tr>
                                                         <tr>
                                                             <td>09:30</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                             <td>Occupied</td>
                                                         </tr>
                                                         <tr>
                                                             <td>10:00</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                         </tr>
                                                         <tr>
                                                             <td>10:30</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                         </tr>
                                                         <tr>
                                                             <td>11:00</td>
                                                             <td>Occupied</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                         </tr>
                                                         <tr>
                                                             <td>11:30</td>
                                                             <td>Occupied</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                         </tr>
                                                         <tr>
                                                             <td>12:00</td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                             <td><a href="rezerwuj">Book</a></td>
                                                         </tr>
                                                     </tbody>
                                                 </table>
                                             </body>
                                         </html>
                                         """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithTimeSlots, 
            _testDate, 
            _testClub, 
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeTrue();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        var court1Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 1").ToList();
        var court2Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 2").ToList();
        
        // Court 1 should have availability from 09:00-11:00 (4 consecutive slots) and 12:00 (1 slot)
        // The 4 consecutive slots should generate multiple duration options
        court1Availabilities.ShouldNotBeEmpty();
        
        // Court 2 should have availability from 10:00-12:00 (4 consecutive slots)
        court2Availabilities.ShouldNotBeEmpty();
        
        // Verify duration options are generated correctly
        var durations = result.CourtAvailabilities.Select(a => a.Duration.TotalMinutes).Distinct().ToList();
        durations.ShouldContain(60);  // 1 hour bookings
        durations.ShouldContain(90);  // 1.5 hour bookings
        durations.ShouldContain(120); // 2 hour bookings
        
        // Verify all availabilities have correct basic properties
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubId == _testClub.ClubId);
        result.CourtAvailabilities.ShouldAllBe(a => a.ClubName == _testClub.Name);
        result.CourtAvailabilities.ShouldAllBe(a => a.Provider == ProviderType.KlubyOrg);
        result.CourtAvailabilities.ShouldAllBe(a => a.BookingUrl.StartsWith(_options.BaseUrl));
        result.CourtAvailabilities.ShouldAllBe(a => !string.IsNullOrEmpty(a.Id));
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime < a.EndTime);
        
        // Verify court types based on name
        var indoorCourts = result.CourtAvailabilities.Where(a => a.CourtName.Contains("hala", StringComparison.InvariantCultureIgnoreCase));
        indoorCourts.ShouldAllBe(a => a.Type == CourtType.Indoor);
        
        var outdoorCourts = result.CourtAvailabilities.Where(a => !a.CourtName.Contains("hala", StringComparison.InvariantCultureIgnoreCase));
        outdoorCourts.ShouldAllBe(a => a.Type == CourtType.Outdoor);
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenHandlingRowspanCells_ShouldCorrectlyMapOccupiedSlots()
    {
        // Arrange
        const string htmlWithRowspanCells = """
                                            <html>
                                                <body>
                                                    <table id="grafik">
                                                        <thead>
                                                            <tr>
                                                                <th>Time</th>
                                                                <th>Court 1</th>
                                                                <th>Court 2</th>
                                                                <th>Court 3</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody>
                                                            <tr>
                                                                <td>09:00</td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <td rowspan="2">Occupied 90min</td>
                                                                <td rowspan="4">Occupied 2hrs</td>
                                                            </tr>
                                                            <tr>
                                                                <td>09:30</td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <!-- Court 2 cell is merged from above -->
                                                                <!-- Court 3 cell is merged from above -->
                                                            </tr>
                                                            <tr>
                                                                <td>10:00</td>
                                                                <td rowspan="2">Occupied 90min</td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <!-- Court 3 cell is merged from above -->
                                                            </tr>
                                                            <tr>
                                                                <td>10:30</td>
                                                                <!-- Court 1 cell is merged from above -->
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <!-- Court 3 cell is merged from above -->
                                                            </tr>
                                                            <tr>
                                                                <td>11:00</td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                            </tr>
                                                            <tr>
                                                                <td>11:30</td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                                <td><a href="rezerwuj">Book</a></td>
                                                            </tr>
                                                        </tbody>
                                                    </table>
                                                </body>
                                            </html>
                                            """;

        // Act
        var result = await _parser.ParseScheduleAsync(
            htmlWithRowspanCells, 
            _testDate, 
            _testClub, 
            _testScheduleUrl);

        // Assert
        result.Success.ShouldBeTrue();
        result.CourtAvailabilities.ShouldNotBeEmpty();
        
        var court1Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 1").ToList();
        var court2Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 2").ToList();
        var court3Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 3").ToList();
        
        // Note: Times are converted from Polish local time to UTC
        // January 2024: Poland (CET) is UTC+1, so 09:00 local → 08:00 UTC
        
        // Court 1: Available 09:00-10:00 Polish (08:00-09:00 UTC), occupied 10:00-11:00, available 11:00-12:00 Polish (10:00-11:00 UTC)
        court1Availabilities.ShouldNotBeEmpty();
        var court1StartTimes = court1Availabilities.Select(a => a.StartTime.TimeOfDay).Distinct().OrderBy(t => t).ToList();
        court1StartTimes.ShouldContain(new TimeSpan(8, 0, 0));   // 09:00 Polish → 08:00 UTC
        court1StartTimes.ShouldContain(new TimeSpan(10, 0, 0));  // 11:00 Polish → 10:00 UTC
        court1StartTimes.ShouldNotContain(new TimeSpan(9, 0, 0)); // Should NOT have 09:00 UTC (10:00 Polish) due to rowspan occupation
        
        // Court 2: Occupied 09:00-10:00 Polish, available 10:00-12:00 Polish (09:00-11:00 UTC)
        court2Availabilities.ShouldNotBeEmpty();
        var court2StartTimes = court2Availabilities.Select(a => a.StartTime.TimeOfDay).Distinct().OrderBy(t => t).ToList();
        court2StartTimes.ShouldContain(new TimeSpan(9, 0, 0));   // 10:00 Polish → 09:00 UTC
        court2StartTimes.ShouldContain(new TimeSpan(10, 0, 0));  // 11:00 Polish → 10:00 UTC
        court2StartTimes.ShouldNotContain(new TimeSpan(8, 0, 0)); // Should NOT have 08:00 UTC (09:00 Polish) due to rowspan occupation
        
        // Court 3: Occupied 09:00-11:00 Polish, available 11:00-12:00 Polish (10:00-11:00 UTC)
        court3Availabilities.ShouldNotBeEmpty();
        var court3StartTimes = court3Availabilities.Select(a => a.StartTime.TimeOfDay).Distinct().OrderBy(t => t).ToList();
        court3StartTimes.ShouldContain(new TimeSpan(10, 0, 0));  // 11:00 Polish → 10:00 UTC
        court3StartTimes.ShouldNotContain(new TimeSpan(8, 0, 0)); // Should NOT have 08:00 UTC (09:00 Polish) due to rowspan occupation
        court3StartTimes.ShouldNotContain(new TimeSpan(9, 0, 0)); // Should NOT have 09:00 UTC (10:00 Polish) due to rowspan occupation
        
        // Verify all availabilities are on the correct date and in UTC
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Date == _testDate.Date);
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Kind == DateTimeKind.Utc);
        result.CourtAvailabilities.ShouldAllBe(a => a.EndTime.Kind == DateTimeKind.Utc);
        
        // Verify that merged cells don't create duplicate availabilities
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime < a.EndTime);
        result.CourtAvailabilities.ShouldAllBe(a => !string.IsNullOrEmpty(a.Id));
    }
    
    [Fact]
    public async Task ParseScheduleAsync_WhenRowspanSpansMultipleAvailabilityPeriods_HandlesCorrectly()
    {
        // Arrange
        var complexRowspanHtml = """
            <html>
                <body>
                    <table id="grafik">
                        <thead>
                            <tr>
                                <th>Time</th>
                                <th>Court 1</th>
                                <th>Court 2</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>09:00</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>09:30</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>10:00</td>
                                <td rowspan="6">Long event (3 hours)</td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>10:30</td>
                                <!-- Court 1 occupied -->
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>11:00</td>
                                <!-- Court 1 occupied -->
                                <td rowspan="2">Event (1 hour)</td>
                            </tr>
                            <tr>
                                <td>11:30</td>
                                <!-- Court 1 occupied -->
                                <!-- Court 2 occupied -->
                            </tr>
                            <tr>
                                <td>12:00</td>
                                <!-- Court 1 occupied -->
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>12:30</td>
                                <!-- Court 1 occupied -->
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>13:00</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>13:30</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>14:00</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                            <tr>
                                <td>14:30</td>
                                <td><a href="rezerwuj">Book</a></td>
                                <td><a href="rezerwuj">Book</a></td>
                            </tr>
                        </tbody>
                    </table>
                </body>
            </html>
            """;

        // Act
        var result = await _parser.ParseScheduleAsync(complexRowspanHtml, _testDate, _testClub, _testScheduleUrl);

        // Assert
        result.Success.ShouldBeTrue();
        
        var court1Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 1").ToList();
        var court2Availabilities = result.CourtAvailabilities.Where(a => a.CourtName == "Court 2").ToList();
        
        // Court 1: Available 09:00-10:00 (before long event), then available from 13:00-15:00 (after long event)
        court1Availabilities.ShouldNotBeEmpty();
        var court1StartTimes = court1Availabilities.Select(a => a.StartTime.TimeOfDay).Distinct().OrderBy(t => t).ToList();
        court1StartTimes.ShouldContain(new TimeSpan(8, 0, 0));   // 09:00 Polish → 08:00 UTC (before event)
        court1StartTimes.ShouldContain(new TimeSpan(12, 0, 0));  // 13:00 Polish → 12:00 UTC (after event)
        court1StartTimes.ShouldContain(new TimeSpan(13, 0, 0));  // 14:00 Polish → 13:00 UTC (after event)
        
        // Verify Court 1 doesn't have slots during the occupied period
        court1StartTimes.ShouldNotContain(new TimeSpan(9, 0, 0));   // 10:00 Polish → 09:00 UTC (during event)
        court1StartTimes.ShouldNotContain(new TimeSpan(10, 0, 0));  // 11:00 Polish → 10:00 UTC (during event)
        court1StartTimes.ShouldNotContain(new TimeSpan(11, 0, 0));  // 12:00 Polish → 11:00 UTC (during event)
        
        // Court 2: Available 09:00-11:00, then available from 12:00-15:00
        court2Availabilities.ShouldNotBeEmpty();
        var court2StartTimes = court2Availabilities.Select(a => a.StartTime.TimeOfDay).Distinct().OrderBy(t => t).ToList();
        court2StartTimes.ShouldContain(new TimeSpan(8, 0, 0));   // 09:00 Polish → 08:00 UTC
        court2StartTimes.ShouldContain(new TimeSpan(11, 0, 0));  // 12:00 Polish → 11:00 UTC (after 1hr event)
        court2StartTimes.ShouldContain(new TimeSpan(12, 0, 0));  // 13:00 Polish → 12:00 UTC
        court2StartTimes.ShouldContain(new TimeSpan(13, 0, 0));  // 14:00 Polish → 13:00 UTC
        
        // Verify Court 2 doesn't have slots during the 1-hour occupied period
        court2StartTimes.ShouldNotContain(new TimeSpan(10, 0, 0)); // 11:00 Polish → 10:00 UTC (during 1hr event)
        
        // Verify that Court 1 has availability both before and after the long event
        var court1BeforeEvent = court1Availabilities.Where(a => a.StartTime.TimeOfDay < new TimeSpan(9, 0, 0)).ToList();
        var court1AfterEvent = court1Availabilities.Where(a => a.StartTime.TimeOfDay >= new TimeSpan(12, 0, 0)).ToList();
        
        court1BeforeEvent.ShouldNotBeEmpty("Court 1 should have availability before the long event");
        court1AfterEvent.ShouldNotBeEmpty("Court 1 should have availability after the long event");
        
        // Verify all availabilities are properly formed
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime < a.EndTime);
        result.CourtAvailabilities.ShouldAllBe(a => !string.IsNullOrEmpty(a.Id));
        result.CourtAvailabilities.ShouldAllBe(a => a.StartTime.Kind == DateTimeKind.Utc);
        result.CourtAvailabilities.ShouldAllBe(a => a.EndTime.Kind == DateTimeKind.Utc);
    }
}
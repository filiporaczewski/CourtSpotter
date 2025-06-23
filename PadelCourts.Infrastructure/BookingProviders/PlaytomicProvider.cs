using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

public class PlaytomicProvider : ICourtProvider
{
    public async Task<IEnumerable<CourtAvailability>> GetCourtAvailabilities(Club club, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        await MockCourtDataGenerator.SimulateApiDelay();
        
        return MockCourtDataGenerator.GenerateAvailabilities(
            club,
            startDate,
            endDate,
            startHour: 7,
            endHour: 22,
            skipProbability: 0.3,
            currency: "PLN",
            courtName: "Court 1",
            bookingUrl: "https://playtomic.io/book",
            basePrice: MockCourtDataGenerator.IsPeakHour(DateTime.Now) ? 100m : 70m,
            provider: club.Provider
        );
    }
}
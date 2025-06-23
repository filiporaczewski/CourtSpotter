using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

public class CourtMeProvider : ICourtProvider
{
    public async Task<IEnumerable<CourtAvailability>> GetCourtAvailabilities(
        Club club, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        await MockCourtDataGenerator.SimulateApiDelay();
        
        return MockCourtDataGenerator.GenerateAvailabilities(
            club,
            startDate,
            endDate,
            startHour: 6,
            endHour: 23,
            skipProbability: 0.2,
            currency: "PLN",
            courtName: "Kort A",
            bookingUrl: "https://court.me/reservation",
            basePrice: 80m,
            provider: club.Provider
        );
    }
}
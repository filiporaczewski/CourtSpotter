using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

public class KlubyOrgProvider : ICourtProvider
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
            startHour: 8,
            endHour: 21,
            skipProbability: 0.4,
            currency: "PLN",
            courtName: "Kort 2",
            bookingUrl: "https://kluby.org/rezerwacje", 
            basePrice: 60m,
            provider: club.Provider
        );
    }
}
using PadelCourts.Core.Contracts;
using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

public class CourtBookingMeProvider : ICourtBookingProvider
{
    public async Task<IEnumerable<CourtAvailability>> GetCourtBookingAvailabilitiesAsync(
        PadelClub padelClub, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        await MockCourtDataGenerator.SimulateApiDelay();
        
        return MockCourtDataGenerator.GenerateAvailabilities(
            padelClub,
            startDate,
            endDate,
            startHour: 6,
            endHour: 23,
            skipProbability: 0.2,
            currency: "PLN",
            courtName: "Kort A",
            bookingUrl: "https://court.me/reservation",
            basePrice: 80m,
            provider: padelClub.Provider
        );
    }
}
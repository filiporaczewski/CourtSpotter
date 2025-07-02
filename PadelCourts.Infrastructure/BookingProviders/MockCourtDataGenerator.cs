using PadelCourts.Core.Models;

namespace PadelCourts.Infrastructure.BookingProviders;

public class MockCourtDataGenerator
{
    private static readonly Random _random = new();
    
    public static async Task SimulateApiDelay()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(500, 2000)));
    }

    public static bool ShouldSkipSlot(double skipProbability = 0.65) // Increased from 0.3 to 0.65
    {
        return _random.NextDouble() < skipProbability;
    }
    
    public static CourtType GetRandomCourtType()
    {
        var types = Enum.GetValues<CourtType>();
        return types[_random.Next(types.Length)];
    }

    public static bool IsPeakHour(DateTime startTime)
    {
        return startTime.Hour >= 18 && startTime.Hour < 21;
    }
    
    private static (TimeSpan duration, decimal price) GetRandomDuration(decimal basePrice)
    {
        var random = _random.NextDouble();

        if (random < 0.6)
        {
            return (TimeSpan.FromHours(1), basePrice);   
        }
        
        if (random < 0.85)
        {
            return (TimeSpan.FromMinutes(90), basePrice * 1.4m);   
        }
        
        return (TimeSpan.FromHours(2), basePrice * 1.7m);
    }

    public static IEnumerable<CourtAvailability> GenerateAvailabilities(
        PadelClub padelClub,
        DateTime startDate,
        DateTime endDate,
        int startHour,
        int endHour,
        double skipProbability,
        string currency,
        string courtName,
        string bookingUrl,
        decimal basePrice,
        ProviderType provider)
    {
        var availabilities = new List<CourtAvailability>();
        var currentDate = startDate.Date;
        
        while (currentDate <= endDate.Date)
        {
            // Reduced time slots - only every hour instead of every 30 minutes
            for (int hour = startHour; hour <= endHour; hour++)
            {
                var startTime = currentDate.AddHours(hour);
                
                if (ShouldSkipSlot(skipProbability)) continue;
                
                // Get only ONE random duration per slot
                var (duration, price) = GetRandomDuration(basePrice);
                
                // Check if the slot fits within operating hours
                if (startTime.Add(duration).Hour > endHour) continue;
                
                availabilities.Add(new CourtAvailability
                {
                    Id = Guid.NewGuid().ToString(),
                    ClubId = padelClub.ClubId,
                    ClubName = padelClub.Name,
                    CourtName = courtName,
                    Type = GetRandomCourtType(),
                    StartTime = startTime,
                    EndTime = startTime.Add(duration),
                    Price = price,
                    Currency = currency,
                    BookingUrl = bookingUrl,
                    Provider = provider
                });
            }
            currentDate = currentDate.AddDays(1);
        }
        
        return availabilities.OrderBy(a => a.StartTime);
    }
}

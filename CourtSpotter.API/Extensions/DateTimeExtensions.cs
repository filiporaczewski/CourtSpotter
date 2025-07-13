namespace CourtSpotter.Extensions;

public static class DateTimeExtensions
{
    public static int CalculateDurationInMinutes(this DateTime startTime, DateTime endTime)
    {
        return (int)(endTime - startTime).TotalMinutes;
    }
}
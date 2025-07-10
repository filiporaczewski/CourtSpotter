namespace CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;

public class GridColumnState
{
    public TimeSpan? StartTime { get; set; }
    
    public int ConsecutiveAvailableRows { get; set; }
    
    public int RemainingRowsBlocked { get; set; }
    
    public string CourtName { get; set; }
}
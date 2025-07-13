using CourtSpotter.Core.Models;

namespace CourtSpotter.Core.Utils;

public static class GetCourtAvailabilitiesQueryTextBuilder
{
    public static string BuildQueryText(DateTime startDate, DateTime endDate, int[]? durationFilters, string[]? clubIds, CourtType? courtType, out Dictionary<string, object> parameters)
    {
        var queryText = "SELECT * FROM c WHERE c.startTime >= @startDate AND c.endTime <= @endDate";
        parameters = new Dictionary<string, object>();
        
        parameters["@startDate"] = startDate;
        parameters["@endDate"] = endDate;

        if (durationFilters != null && durationFilters.Length > 0)
        {
            var durationTimeSpans = durationFilters.Select(minutes => TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm\:ss")).ToArray();
            var durationConditions = new List<string>();
            
            for (int i = 0; i < durationTimeSpans.Length; i++)
            {
                var paramName = $"@duration{i}";
                durationConditions.Add($"c.duration = {paramName}");
                parameters[paramName] = durationTimeSpans[i];
            }
            
            queryText += $" AND ({string.Join(" OR ", durationConditions)})";
        }

        if (clubIds != null && clubIds.Length > 0)
        {
            var clubIdsConditions = new List<string>();
            for (int i = 0; i < clubIds.Length; i++)
            {
                var paramName = $"@clubId{i}";
                clubIdsConditions.Add($"c.clubId = {paramName}");
                parameters[paramName] = clubIds[i];
            }
            
            queryText += $" AND ({string.Join(" OR ", clubIdsConditions)})";       
        }

        if (courtType != null)
        {
            queryText += " AND c.type = @courtType";
            parameters["@courtType"] = (int)courtType;
        }

        return queryText;
    }
}
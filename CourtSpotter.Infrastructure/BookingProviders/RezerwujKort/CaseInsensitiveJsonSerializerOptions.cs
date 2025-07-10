using System.Text.Json;

namespace CourtSpotter.Infrastructure.BookingProviders.RezerwujKort;

public class CaseInsensitiveJsonSerializerOptions
{
    public JsonSerializerOptions Value { get; private set; }

    public CaseInsensitiveJsonSerializerOptions()
    {
        Value = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
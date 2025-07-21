namespace CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;

public class KlubyOrgProviderOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://kluby.org/";
}
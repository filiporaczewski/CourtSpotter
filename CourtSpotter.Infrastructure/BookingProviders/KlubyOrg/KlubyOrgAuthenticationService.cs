using System.Net;
using CourtSpotter.Core.Contracts;
using Microsoft.Extensions.Options;

namespace CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;

public class KlubyOrgAuthenticationService : IKlubyOrgAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly string _klubyOrgLogin;
    private readonly string _klubyOrgPassword;
    private readonly string _baseUrl;

    private const string UsernameFormParamName = "konto";
    private const string PasswordFormParamName = "haslo";
    private const string LoginActionParamName = "logowanie";
    private const string RememberMeParamName = "remember";
    private const string RedirectPageParamName = "page";
    private const string LoginEndpoint = "logowanie";
    private const string AuthCookieName = "kluby_autolog";

    public KlubyOrgAuthenticationService(HttpClient httpClient, CookieContainer cookieContainer, IOptions<KlubyOrgProviderOptions> options)
    {
        _httpClient = httpClient;
        _cookieContainer = cookieContainer;
        var optionsValue = options.Value; 
        _klubyOrgLogin = optionsValue.Username;
        _klubyOrgPassword = optionsValue.Password;
        _baseUrl = optionsValue.BaseUrl;
    }
    
    public async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (IsAuthenticated())
        {
            return true;
        }
        
        await LoginToKlubyOrgAsync(_klubyOrgLogin, _klubyOrgPassword, cancellationToken);
        return IsAuthenticated();
    }

    private bool IsAuthenticated()
    {
        var cookies = _cookieContainer.GetCookies(new Uri(_baseUrl));
        return cookies.Any(c => c.Name == AuthCookieName);
    }

    private async Task LoginToKlubyOrgAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var loginParams = new KeyValuePair<string, string>[]
        {
            new(UsernameFormParamName, username),
            new(PasswordFormParamName, password),
            new(LoginActionParamName, "1"),
            new(RememberMeParamName, "1"),
            new(RedirectPageParamName, "/")
        };

        var loginFormUrlEncodedContent = new FormUrlEncodedContent(loginParams);
        await _httpClient.PostAsync(LoginEndpoint, loginFormUrlEncodedContent, cancellationToken);
    }
}
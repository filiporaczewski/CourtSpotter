using System.Net;
using CourtSpotter.Infrastructure.BookingProviders.KlubyOrg;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Shouldly;

namespace CourtSpotter.Infrastructure.Tests.KlubyOrgProvider;

public class KlubyOrgAuthenticationServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly CookieContainer _cookieContainer;
    private readonly KlubyOrgAuthenticationService _authService;
    
    private const string TestBaseUrl = "https://kluby.org/";
    private const string TestUsername = "test-user";
    private const string TestPassword = "test-password";
    private const string AuthCookieName = "kluby_autolog";
    private const string HttpClientName = "KlubyOrgClient";
    
    public KlubyOrgAuthenticationServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _cookieContainer = new CookieContainer();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(TestBaseUrl),
        };
        
        httpClientFactoryMock.Setup(f => f.CreateClient(HttpClientName)).Returns(httpClient);

        var options = new KlubyOrgProviderOptions
        {
            BaseUrl = TestBaseUrl,
            Username = TestUsername,
            Password = TestPassword
        };

        var optionsMock = new Mock<IOptions<KlubyOrgProviderOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        _authService = new KlubyOrgAuthenticationService(httpClientFactoryMock.Object, _cookieContainer, optionsMock.Object);
    }

    [Fact]
    public async Task EnsureAuthenticatedAsync_WhenUserIsAlreadyLoggedIn_ShouldSkipAuthenticationAndReturnTrue()
    {
        // Arrange
        AddAuthCookieToCookieContainer();

        // Act
        var result = await _authService.EnsureAuthenticatedAsync();

        // Assert
        result.ShouldBeTrue();
        VerifyNoHttpRequestsMade();
    }
    
    [Fact]
    public async Task EnsureAuthenticatedAsync_WhenUserIsNotLoggedInAndAuthenticationSucceeds_ShouldAttemptAuthenticationAndReturnTrue()
    {
        // Arrange
        SetupSuccessfulLoginResponse();

        // Act
        var result = await _authService.EnsureAuthenticatedAsync();

        // Assert
        result.ShouldBeTrue();
        VerifyLoginRequestWasMade();
    }
    
    [Fact]
    public async Task EnsureAuthenticatedAsync_WhenAuthenticationFails_ShouldReturnFalse()
    {
        // Arrange
        SetupFailedLoginResponse();

        // Act
        var result = await _authService.EnsureAuthenticatedAsync();

        // Assert
        result.ShouldBeFalse();
        VerifyLoginRequestWasMade();
    }

    private void SetupFailedLoginResponse()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        // Note: No auth cookie is added, simulating failed authentication
    }
    
    private void SetupSuccessfulLoginResponse()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback(AddAuthCookieToCookieContainer);
    }
    
    private void AddAuthCookieToCookieContainer()
    {
        _cookieContainer.Add(new Cookie(AuthCookieName, "some-value", "/", "kluby.org"));
    }
    
    private void VerifyLoginRequestWasMade()
    {
        _httpMessageHandlerMock.Protected().Verify(
    "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri!.ToString().Contains("logowanie")),
                ItExpr.IsAny<CancellationToken>());
    }

    
    private void VerifyNoHttpRequestsMade()
    {
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
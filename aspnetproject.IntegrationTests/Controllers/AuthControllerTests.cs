using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class AuthControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessTokenAndSetsRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new
        {
            UniqueIdentifier = "alice",
            Password = TestDataSeeder.SharedPlaintextPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out _));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericBadRequest()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new
        {
            UniqueIdentifier = "alice",
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForDeactivatedUser_IsRejected()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new
        {
            
            UniqueIdentifier = "erin_deactivated",
            Password = TestDataSeeder.SharedPlaintextPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /auth/refresh ----------
    // The refresh cookie is Secure, so the test client (plain http) never sends it back on its own;
    // tests attach the Cookie header by hand.

    [Fact]
    public async Task Refresh_WithoutRefreshCookie_ReturnsUnauthorizedEnvelope()
    {
        var response = await _client.PostAsync("api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.False(body!.Succeeded);
        Assert.Equal(ResponseMessages.NoRefreshTokenProvided, body.Message);
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsUnauthorizedEnvelope()
    {
        var response = await PostRefreshAsync("not-a-real-refresh-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.False(body!.Succeeded);
        Assert.Equal(ResponseMessages.RefreshTokenNotFound, body.Message);
    }

    [Fact]
    public async Task Refresh_WithRevokedRefreshToken_ReturnsUnauthorizedEnvelope()
    {
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new
        {
            UniqueIdentifier = "alice",
            Password = TestDataSeeder.SharedPlaintextPassword
        });
        var refreshToken = ReadRefreshTokenCookie(loginResponse);

        var firstRefresh = await PostRefreshAsync(refreshToken); // rotates: the first token is now revoked
        var secondRefresh = await PostRefreshAsync(refreshToken);

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);

        var body = await secondRefresh.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.False(body!.Succeeded);
        Assert.Equal(ResponseMessages.RefreshTokenNotFound, body.Message);
    }

    private async Task<HttpResponseMessage> PostRefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={refreshToken}");

        return await _client.SendAsync(request);
    }

    private static string ReadRefreshTokenCookie(HttpResponseMessage loginResponse)
    {
        var setCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("refreshToken="));

        return setCookie["refreshToken=".Length..].Split(';')[0];
    }
}

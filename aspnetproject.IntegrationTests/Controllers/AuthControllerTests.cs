using System.Net;
using System.Net.Http.Json;
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
}

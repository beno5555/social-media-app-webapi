using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.RateLimiting;

// Limiter state lives in the host's singleton RateLimiter, so exhausting a policy here must never
// leak into other tests. xUnit builds a new instance of this class per test, and each instance owns
// (and disposes) its own factory, so the exhausted limiter dies with the test whatever the run order.
// The requests below never touch the database, so no reseed is needed.
[Collection("Database")]
public class RateLimitRejectionTests : IAsyncLifetime
{
    // One more than the old limit of 5: six page reloads in a row must all get through.
    private const int ReloadsInARow = 6;

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RateLimitRejectionTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- Refresh policy (per IP) ----------
    // Without a refresh cookie the endpoint answers 401 until the policy runs out of permits.

    [Fact]
    public async Task Refresh_WhenPolicyIsExhausted_ReturnsTooManyRequestsEnvelope()
    {
        var response = await RateLimitExhaustHelper.ExhaustAsync(
            () => _client.PostAsync("api/auth/refresh", null), HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.False(body!.Succeeded);
        Assert.Equal(ResponseMessages.TooManyRequests, body.Message);
    }

    [Fact]
    public async Task Refresh_OnSixthCallFromOneAddress_IsNotRejectedWithTooManyRequests()
    {
        HttpResponseMessage response = null!;

        for (var reload = 0; reload < ReloadsInARow; reload++)
        {
            response = await _client.PostAsync("api/auth/refresh", null);
        }

        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}

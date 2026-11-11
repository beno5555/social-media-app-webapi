using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.IntegrationTests.RateLimiting;

// Limiter state lives in the host's singleton RateLimiter, so exhausting a policy here must never
// leak into other tests. xUnit builds a new instance of this class per test, and each instance owns
// (and disposes) its own factory, so the exhausted limiter dies with the test whatever the run order.
// The requests below never touch the database, so no reseed is needed.
[Collection("Database")]
public class RateLimitRejectionTests : IAsyncLifetime
{
    // Comfortably above the Refresh policy's permit limit, so the loop always reaches a rejection.
    private const int MaxAttempts = 50;

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
        HttpResponseMessage response;
        var attempts = 0;
        bool stillAllowed;

        do
        {
            response = await _client.PostAsync("api/auth/refresh", null);
            attempts++;

            stillAllowed = response.StatusCode == HttpStatusCode.Unauthorized;
        }
        while (stillAllowed && attempts < MaxAttempts);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        Assert.False(body!.Succeeded);
        Assert.Equal(ResponseMessages.TooManyRequests, body.Message);
    }
}

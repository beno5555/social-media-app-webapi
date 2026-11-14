using System.Net;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.RateLimiting;

// Every request from the test client has the same remote address (the test host sets none, so all of them share the
// limiter's IP fallback key), so these tests show which partition a request counts against: a signed-in caller's own
// budget, or the shared per-IP one.
// Limiter state lives in the host's singleton RateLimiter. xUnit builds a new instance of this class per test, and each
// instance owns (and disposes) its own factory, so an exhausted limiter dies with the test whatever the run order.
[Collection("Database")]
public class RateLimitPartitionTests : IAsyncLifetime
{
    // Comfortably above the SearchUsers policy's permit limit, so the loop always reaches a rejection,
    // and far below the global limiter's, so only the policy under test can reject.
    private const int MaxAttempts = 50;

    // SearchUsers is a per-user policy on an endpoint that also allows anonymous callers, and it only reads.
    private const string SearchUsersUrl = "api/users/search?username=a";

    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RateLimitPartitionTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- SearchUsers policy (per user) ----------
    // alice (2) and bob (3) are active seeded users signing in from the same test IP.

    [Fact]
    public async Task SearchUsers_WhenOneUserExhaustsPolicy_StillAllowsAnotherUserFromSameIp()
    {
        var aliceToken = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        var bobToken   = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);

        AuthTestHelper.AttachToken(_client, aliceToken);
        var aliceLastResponse = await ExhaustSearchUsersAsync();

        AuthTestHelper.AttachToken(_client, bobToken);
        var bobResponse = await _client.GetAsync(SearchUsersUrl);

        Assert.Equal(HttpStatusCode.TooManyRequests, aliceLastResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bobResponse.StatusCode);
    }

    // An expired token for alice must not buy her own budget: authentication rejects it, so the request
    // counts against the IP partition that anonymous callers have already used up.
    [Fact]
    public async Task SearchUsers_WithExpiredTokenAfterAnonymousCallersExhaustPolicy_ReturnsTooManyRequests()
    {
        var anonymousLastResponse = await ExhaustSearchUsersAsync();

        AuthTestHelper.AttachToken(_client, ExpiredTokenFactory.Create(_factory, userId: 2)); // alice
        var expiredTokenResponse = await _client.GetAsync(SearchUsersUrl);

        Assert.Equal(HttpStatusCode.TooManyRequests, anonymousLastResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, expiredTokenResponse.StatusCode);
    }

    // Sends SearchUsers requests with the client's current token until one is rejected, and returns the last response.
    private async Task<HttpResponseMessage> ExhaustSearchUsersAsync()
    {
        HttpResponseMessage response;
        var attempts = 0;
        bool stillAllowed;

        do
        {
            response = await _client.GetAsync(SearchUsersUrl);
            attempts++;

            stillAllowed = response.StatusCode == HttpStatusCode.OK;
        }
        while (stillAllowed && attempts < MaxAttempts);

        return response;
    }
}

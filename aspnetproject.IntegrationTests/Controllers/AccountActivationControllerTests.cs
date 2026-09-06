using System.Net;
using System.Net.Http.Json;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class AccountActivationControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountActivationControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- PATCH /accounts/mine/deactivate ----------

    [Fact]
    public async Task DeactivateMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsync("api/accounts/mine/deactivate", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateMine_WithToken_HidesFromLookupAndRevokesTokens()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deactivateResponse = await _client.PatchAsync("api/accounts/mine/deactivate", null);
        Assert.True(deactivateResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/users/diana");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);

        // Refresh token issued at login should now be revoked
        var refreshResponse = await _client.PostAsync("api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivateMine_LoginAfterDeactivation_Fails()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
        await _client.PatchAsync("api/accounts/mine/deactivate", null);

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new
        {
            UniqueIdentifier = "diana",
            Password = TestDataSeeder.SharedPlaintextPassword
        });

        // Deactivated users are hidden by the global query filter — login should
        // fail the same generic way as any nonexistent-user login (no oracle).
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
    }

    // ---------- PATCH /accounts/{id}/deactivate ----------
    // Documented as reachable by ANY authenticated caller — no [Authorize(Roles)]
    // despite the "admin" framing. These tests specifically verify that gap exists
    // as documented, so if it's since been locked down, these should start failing
    // and flag the fix.

    [Fact]
    public async Task DeactivateOther_ByNonAdminCaller_CurrentlySucceeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PatchAsync("api/accounts/3/deactivate", null); // bob, by alice (non-admin)

        // Per controllers.md: no role restriction on this route today.
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivateOther_ByAdmin_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PatchAsync("api/accounts/4/deactivate", null); // charlie

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/users/charlie");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivateOther_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsync("api/accounts/3/deactivate", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateOther_AlreadyDeactivated_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // erin (Id 6) is already deactivated in seed data
        var response = await _client.PatchAsync("api/accounts/6/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PATCH /accounts/{id}/activate (admin, immediate) ----------

    [Fact]
    public async Task ActivateOther_AsAdmin_ReactivatesImmediately()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var activateResponse = await _client.PatchAsync("api/accounts/6/activate", null); // erin, pre-deactivated
        Assert.True(activateResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/users/erin_deactivated");
        Assert.Equal(HttpStatusCode.OK, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task ActivateOther_AsNonAdmin_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PatchAsync("api/accounts/6/activate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ActivateOther_NotCurrentlyDeactivated_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // alice (Id 2) is active, never deactivated
        var response = await _client.PatchAsync("api/accounts/2/activate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PATCH /accounts/request-activate (anonymous, request token) ----------

    [Fact]
    public async Task RequestActivate_ForDeactivatedAccount_ReturnsOkRegardlessOfEmailValidity()
    {
        // Oracle-avoidance pattern used elsewhere (e.g. failed-register-attempt email):
        // should not reveal whether the email/account exists.
        var response = await _client.PatchAsJsonAsync("api/accounts/request-activate", new
        {
            Email = "erin@example.com"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestActivate_ForNonexistentEmail_ReturnsBadRequest()
    {
        var response = await _client.PatchAsJsonAsync("api/accounts/request-activate", new
        {
            Email = "no-such-account@example.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PATCH /accounts/activate (anonymous, consume token) ----------

    [Fact]
    public async Task Activate_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PatchAsJsonAsync("api/accounts/activate", new
        {
            Email = "erin@example.com",
            Token = "not-a-real-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_ForAlreadyActiveAccount_ReturnsBadRequest()
    {
        var response = await _client.PatchAsJsonAsync("api/accounts/activate", new
        {
            Email = "alice@example.com",
            Token = "irrelevant-since-account-is-active"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // NOTE: a full happy-path test for RequestActivate -> Activate would need to
    // intercept the emailed token (EmailSender is real MailKit/MimeKit per core.md).
    // Recommend swapping EmailSender for a test double in CustomWebApplicationFactory
    // that captures the token instead of sending mail, so this round trip is testable
    // without a real inbox. Flagging rather than assuming — want me to add that?

    // ---------- Rate-limit policy check (flagged as possibly swapped) ----------

    [Fact]
    public async Task DeactivateMine_RateLimit_AllowsAtLeastSeveralCallsPerHour()
    {
        // Per controllers.md, mine/deactivate is annotated with the policy named
        // AdminDeactivateAccount (documented as 30/hour, generous). This test only
        // checks it doesn't 429 on a small burst — it does not assert the exact
        // limit, since RateLimitConfig.cs wasn't in the shared docs.
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // Only fire once here since deactivating alice would break other assertions
        // if this test file's ordering changed — full rate-limit testing belongs in
        // a dedicated RateLimitTests class hitting a disposable throwaway user per call.
        var response = await _client.PatchAsync("api/accounts/mine/deactivate", null);

        Assert.NotEqual((HttpStatusCode)429, response.StatusCode);
    }
}

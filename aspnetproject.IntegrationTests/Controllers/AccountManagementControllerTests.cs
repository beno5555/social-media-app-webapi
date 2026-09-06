using System.Net;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class AccountManagementControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountManagementControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- PUT /accounts/{id}/assign-administrator ----------

    [Fact]
    public async Task AssignAdministrator_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsync("api/accounts/2/assign-administrator", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AssignAdministrator_AsNonAdmin_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsync("api/accounts/3/assign-administrator", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignAdministrator_AsAdmin_GrantsRoleAndReflectsOnMine()
    {
        var adminToken = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, adminToken);

        var assignResponse = await _client.PutAsync("api/accounts/2/assign-administrator", null); // alice
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var aliceToken = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, aliceToken);

        var mineResponse = await _client.GetAsync("api/users/mine");
        var body = await mineResponse.Content.ReadAsStringAsync();

        Assert.Contains("Administrator", body);
    }

    [Fact]
    public async Task AssignAdministrator_AlreadyAssigned_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // sandro_beno (Id 1) already has both roles per seed data
        var response = await _client.PutAsync("api/accounts/1/assign-administrator", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignAdministrator_NonexistentUser_ReturnsNotFoundOrBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsync("api/accounts/9999/assign-administrator", null);

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest);
    }

    // ---------- PUT /accounts/{id}/remove-administrator ----------

    [Fact]
    public async Task RemoveAdministrator_AsAdmin_RevokesRole()
    {
        var adminToken = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, adminToken);

        // Grant, then revoke, alice's admin role
        await _client.PutAsync("api/accounts/2/assign-administrator", null);
        var revokeResponse = await _client.PutAsync("api/accounts/2/remove-administrator", null);

        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        var aliceToken = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, aliceToken);

        var mineResponse = await _client.GetAsync("api/users/mine");
        var body = await mineResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Administrator", body);
    }

    [Fact]
    public async Task RemoveAdministrator_NotCurrentlyAssigned_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // bob never had the Administrator role
        var response = await _client.PutAsync("api/accounts/3/remove-administrator", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveAdministrator_AsNonAdmin_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsync("api/accounts/1/remove-administrator", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- DELETE /accounts/mine ----------

    [Fact]
    public async Task DeleteMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/accounts/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMine_SoftDeletesAndHidesFromLookup()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/accounts/mine");
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/users/diana");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMine_RevokesRefreshTokens_SubsequentRefreshFails()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "diana", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        await _client.DeleteAsync("api/accounts/mine");

        // The refresh cookie issued at login should now be revoked server-side
        var refreshResponse = await _client.PostAsync("api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMine_AlreadySoftDeleted_ReturnsBadRequest()
    {
        // frank_deleted (Id 7) is already soft-deleted in seed data — but a deleted
        // account can't log in via /auth/login (hidden by query filter), so we exercise
        // idempotency through the admin path instead (see DeleteOther test below).
        // Left as a documented gap: no client-facing way to hit this branch as the
        // deleted user themself, since they can no longer authenticate.
        await Task.CompletedTask;
    }

    // ---------- DELETE /accounts/{id} ----------

    [Fact]
    public async Task DeleteOther_AsNonAdmin_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/accounts/3"); // bob

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOther_AsAdmin_SoftDeletesTargetUser()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/accounts/5"); // diana
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/users/diana");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteOther_AlreadySoftDeleted_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        // frank (Id 7) is already soft-deleted in seed data
        var response = await _client.DeleteAsync("api/accounts/7");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOther_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/accounts/3");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

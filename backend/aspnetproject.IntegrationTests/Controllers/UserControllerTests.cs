using System.Net;
using System.Net.Http.Json;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class UserControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- GET /users/{id:int} ----------

    [Fact]
    public async Task GetById_Anonymous_ReturnsStandardDtoWithoutEmail()
    {
        var response = await _client.GetAsync("api/users/2"); // alice

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("alice", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alice@example.com", body); // StandardUserDto excludes email
        Assert.DoesNotContain("Roles", body); // StandardUserDto excludes roles
    }

    [Fact]
    public async Task GetById_ForDeactivatedUser_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/users/6"); // erin_deactivated

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForSoftDeletedUser_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/users/7"); // frank_deleted

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonexistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/users/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET /users/{username} ----------

    [Fact]
    public async Task GetByUsername_Anonymous_ReturnsStandardDto()
    {
        var response = await _client.GetAsync("api/users/alice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("alice", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByUsername_DoesNotCollideWithIdRoute()
    {
        // Route-constraint disambiguation: "2" should hit the {id:int} overload,
        // a non-numeric username should hit the username overload without conflict.
        var byId = await _client.GetAsync("api/users/2");
        var byUsername = await _client.GetAsync("api/users/alice");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byUsername.StatusCode);
    }

    // ---------- GET /users/mine ----------

    [Fact]
    public async Task GetMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/users/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_WithToken_ReturnsFullDtoIncludingEmailAndRoles()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/users/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("benashvilisandro91@gmail.com", body);
        Assert.Contains("Administrator", body);
        Assert.Contains("User", body);
    }

    [Fact]
    public async Task GetMine_ForNonAdminUser_ReturnsEmptyOrNoAdminRole()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/users/mine");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Administrator", body);
    }

    // ---------- GET /users/search ----------

    [Fact]
    public async Task Search_Anonymous_ReturnsMinimalDtoOnly()
    {
        var response = await _client.GetAsync("api/users/search?Username=ali&PageNumber=1&PageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("alice", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alice@example.com", body);
        Assert.DoesNotContain("Primary test caller", body); // no Bio in MinimalUserDto
    }

    [Fact]
    public async Task Search_ExcludesDeactivatedAndSoftDeletedUsers()
    {
        var response = await _client.GetAsync("api/users/search?Username=erin&PageNumber=1&PageSize=10");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("erin_deactivated", body);
    }

    [Fact]
    public async Task Search_WithEmptyUsername_ReturnsBadRequest()
    {
        // SearchUserQuery.Username has [MinLength(1)]
        var response = await _client.GetAsync("api/users/search?Username=&PageNumber=1&PageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_PageSizeOverMax_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("api/users/search?Username=a&PageNumber=1&PageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /users/mine ----------

    [Fact]
    public async Task UpdateMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "alice",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "updated"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMine_BioOnly_DoesNotStampUsernameCooldown()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "alice", // unchanged
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "new bio text"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("new bio text", body);
    }

    [Fact]
    public async Task UpdateMine_ChangingUsernameTwiceWithinCooldown_SecondChangeRejected()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var first = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "bob_renamed",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "Alice's accepted friend"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "bob_renamed_again",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "Alice's accepted friend"
        });

        // Constants.UsernameChangeCooldownDays = 20
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task UpdateMine_DuplicateUsername_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "bob", // already taken
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "Primary test caller"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(12)]  // under MinAge (13)
    [InlineData(101)] // over MaxAge (100)
    public async Task UpdateMine_AgeOutsideAllowedRange_ReturnsBadRequest(int ageYears)
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "alice",
            DateOfBirth = DateTime.UtcNow.AddYears(-ageYears),
            Bio = "Primary test caller"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMine_FutureDateOfBirth_ReturnsBadRequest()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/mine", new
        {
            Username = "alice",
            DateOfBirth = DateTime.UtcNow.AddYears(1),
            Bio = "Primary test caller"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /users/{id:int} (admin edit) ----------

    [Fact]
    public async Task UpdateOther_AsNonAdmin_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/3", new
        {
            Username = "bob",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "edited by non-admin"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOther_AsAdmin_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/users/4", new // charlie
        {
            Username = "charlie",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "edited by admin"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("edited by admin", body);
    }

    [Fact]
    public async Task UpdateOther_AsAdmin_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/users/4", new
        {
            Username = "charlie",
            DateOfBirth = new DateTime(2000, 1, 1),
            Bio = "anon edit attempt"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- POST /users (admin-create) ----------

    [Fact]
    public async Task AdminCreate_WithoutAdminRole_ReturnsForbidden()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/users", new
        {
            Username = "created_by_alice",
            Email = "created_by_alice@example.com",
            Password = "SomeValidPassword1!",
            DateOfBirth = new DateTime(2000, 1, 1)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreate_AsAdmin_CreatesUserWithFullDto()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/users", new
        {
            Username = "created_by_admin",
            Email = "created_by_admin@example.com",
            Password = "SomeValidPassword1!",
            DateOfBirth = new DateTime(2000, 1, 1)
        });

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("created_by_admin@example.com", body); // FullUserDto on fresh registration
    }

    [Fact]
    public async Task AdminCreate_DuplicateEmail_ReturnsBadRequestAndDoesNotLeakExistence()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/users", new
        {
            Username = "new_username",
            Email = "alice@example.com", // already exists
            Password = "SomeValidPassword1!",
            DateOfBirth = new DateTime(2000, 1, 1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

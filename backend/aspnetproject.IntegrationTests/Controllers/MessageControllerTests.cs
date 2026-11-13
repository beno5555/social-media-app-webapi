using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class MessageControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public MessageControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new CustomWebApplicationFactory();
        _client    = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    // Disposing the host releases its settings-file watchers; each one holds an inotify instance, and Linux caps them per user.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task LoginAsAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    // ---------- PUT /messages/{id} ----------
    // Message 3: alice (2) → bob (3), sent at seed time, so inside the edit window.
    // Message 5: alice (2) → bob (3), sent on 2026-09-01, so past the edit window.
    // charlie (4) is in neither conversation.

    [Fact]
    public async Task EditMessage_BySenderWithinWindow_ReturnsEditedMessage()
    {
        await LoginAsAsync("alice");

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Corrected message from Alice to Bob."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardMessageDto>>();
        Assert.Equal(3, body!.Data!.Id);
        Assert.Equal("Corrected message from Alice to Bob.", body.Data.Content);
        Assert.True(body.Data.IsEdited);
    }

    [Fact]
    public async Task EditMessage_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Anonymous edit attempt."
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EditMessage_BySenderPastWindow_ReturnsBadRequestWithWindowExpired()
    {
        await LoginAsAsync("alice");

        var response = await _client.PutAsJsonAsync("api/messages/5", new EditMessageDto
        {
            NewContent = "Too late to correct this."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardMessageDto>>();
        Assert.Equal(ResponseMessages.EditWindowExpired, body!.Message);
    }

    [Fact]
    public async Task EditMessage_ByReceiver_ReturnsBadRequestWithNoPermission()
    {
        await LoginAsAsync("bob");

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Bob rewrites Alice's words."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardMessageDto>>();
        Assert.Equal(ResponseMessages.DoNotHavePermissionToEditMessage, body!.Message);
    }

    [Fact]
    public async Task EditMessage_ByOutsider_ReturnsBadRequestWithInvalidRequest()
    {
        await LoginAsAsync("charlie");

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Charlie was never in this conversation."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardMessageDto>>();
        Assert.Equal(ResponseMessages.InvalidRequest, body!.Message);
    }

    [Fact]
    public async Task EditMessage_ForNonexistentMessage_ReturnsBadRequestWithNotFound()
    {
        await LoginAsAsync("alice");

        var response = await _client.PutAsJsonAsync("api/messages/9999", new EditMessageDto
        {
            NewContent = "Nothing to edit."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardMessageDto>>();
        Assert.Equal(ResponseMessages.MessageNotFound, body!.Message);
    }
}

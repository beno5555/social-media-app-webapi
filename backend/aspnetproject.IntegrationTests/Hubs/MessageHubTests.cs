using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Hubs;

// What a connected user receives over hubs/messages when another user acts through HTTP.
[Collection("Database")]
public class MessageHubTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public MessageHubTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new CustomWebApplicationFactory();
        _client    = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    // Disposing the host closes any hub connection a test left open.
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ---------- POST /messages/{receiverId} → ReceiveMessage ----------
    // alice (2) and bob (3) are accepted friends.
    // charlie (4) has no accepted friendship, so he receives no presence events either.

    [Fact]
    public async Task SendMessage_ToFriend_DeliversReceiveMessageToReceiver()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/messages/3", new CreateMessageDto // to bob
        {
            Content = "Bob, this one arrives live."
        });
        var pushed = await bob.WaitForEventAsync<PushMessageDto>(HubEvents.ReceiveMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<SentMessageDto>>();
        Assert.Equal(body!.Data!.Id, pushed.Id);
        Assert.Equal("Bob, this one arrives live.", pushed.Content);
        Assert.Equal(2, pushed.Sender.Id); // alice
        Assert.Equal("alice", pushed.Sender.Username);
    }

    [Fact]
    public async Task SendMessage_ToFriend_DeliversNothingToThirdUser()
    {
        await using var bob     = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var charlie = await HubTestClient.ConnectAsync(_factory, "charlie");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsJsonAsync("api/messages/3", new CreateMessageDto // to bob
        {
            Content = "Only Bob should see this."
        });
        await bob.WaitForEventAsync<PushMessageDto>(HubEvents.ReceiveMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await charlie.AssertNoEventAsync(HubEvents.ReceiveMessage);
    }

    // ---------- PUT /messages/{id} → MessageEdited ----------
    // Message 3: alice (2) → bob (3), inside the edit window. Message 5: alice → bob, past the edit window.

    [Fact]
    public async Task EditMessage_WithinWindow_DeliversMessageEditedToReceiver()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Bob, I meant this."
        });
        var pushed = await bob.WaitForEventAsync<MessageEditedDto>(HubEvents.MessageEdited);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, pushed.Id);
        Assert.Equal("Bob, I meant this.", pushed.Content);
    }

    [Fact]
    public async Task EditMessage_WithinWindow_DeliversNothingToEditorOrThirdUser()
    {
        await using var alice   = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob     = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var charlie = await HubTestClient.ConnectAsync(_factory, "charlie");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto
        {
            NewContent = "Only Bob should see this edit."
        });
        await bob.WaitForEventAsync<MessageEditedDto>(HubEvents.MessageEdited);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await alice.AssertNoEventAsync(HubEvents.MessageEdited);
        await charlie.AssertNoEventAsync(HubEvents.MessageEdited);
    }

    [Fact]
    public async Task EditMessage_PastWindow_DeliversNothing()
    {
        await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/messages/5", new EditMessageDto
        {
            NewContent = "Too late to correct this."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await alice.AssertNoEventAsync(HubEvents.MessageEdited);
        await bob.AssertNoEventAsync(HubEvents.MessageEdited);
    }

    [Fact]
    public async Task EditMessage_ByReceiver_DeliversNothing()
    {
        await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/messages/3", new EditMessageDto // alice's message
        {
            NewContent = "Bob rewrites Alice's words."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await alice.AssertNoEventAsync(HubEvents.MessageEdited);
        await bob.AssertNoEventAsync(HubEvents.MessageEdited);
    }

    // ---------- SendTyping (hub method) → UserTyping ----------
    // alice (2) and bob (3) are accepted friends. charlie (4) has only a pending request to alice.

    [Fact]
    public async Task SendTyping_ToFriend_DeliversUserTypingToFriend()
    {
        await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");

        await alice.InvokeAsync(HubMethods.SendTyping, 3); // to bob
        var pushed = await bob.WaitForEventAsync<UserTypingDto>(HubEvents.UserTyping);

        Assert.Equal(2, pushed.UserId); // alice
    }

    [Fact]
    public async Task SendTyping_ToFriend_DeliversNothingToSenderOrThirdUser()
    {
        await using var alice   = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob     = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var charlie = await HubTestClient.ConnectAsync(_factory, "charlie");

        await alice.InvokeAsync(HubMethods.SendTyping, 3); // to bob
        await bob.WaitForEventAsync<UserTypingDto>(HubEvents.UserTyping);

        await alice.AssertNoEventAsync(HubEvents.UserTyping);
        await charlie.AssertNoEventAsync(HubEvents.UserTyping);
    }

    // InvokeAsync throws if the server reports an error, so each call completing is the "no error" half.
    [Theory]
    [InlineData(4)]   // charlie: pending request only
    [InlineData(999)] // no such user
    [InlineData(2)]   // alice herself
    public async Task SendTyping_ToNonFriendUnknownOrSelf_DeliversNothing(int receiverId)
    {
        await using var alice   = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob     = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var charlie = await HubTestClient.ConnectAsync(_factory, "charlie");

        await alice.InvokeAsync(HubMethods.SendTyping, receiverId);

        await alice.AssertNoEventAsync(HubEvents.UserTyping);
        await bob.AssertNoEventAsync(HubEvents.UserTyping);
        await charlie.AssertNoEventAsync(HubEvents.UserTyping);
    }

    [Fact]
    public async Task SendTyping_ToFriendOrNonFriend_WritesNoLog()
    {
        await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword); // administrator
        AuthTestHelper.AttachToken(_client, token);
        var logIdsBefore = await GetLogIdsAsync();

        await alice.InvokeAsync(HubMethods.SendTyping, 3); // to bob
        await bob.WaitForEventAsync<UserTypingDto>(HubEvents.UserTyping);
        await alice.InvokeAsync(HubMethods.SendTyping, 4); // to charlie, not a friend
        var logIdsAfter = await GetLogIdsAsync();

        Assert.Equal(logIdsBefore, logIdsAfter);
    }

    // Page size 100 holds every seeded row plus the ones the sign-ins above wrote.
    private async Task<List<int>> GetLogIdsAsync()
    {
        var response = await _client.GetAsync("api/admin/logs?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ListResponse<LogDisplayDto>>();
        return body!.Data.Select(log => log.Id).ToList();
    }
}

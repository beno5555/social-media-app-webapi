using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
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
}

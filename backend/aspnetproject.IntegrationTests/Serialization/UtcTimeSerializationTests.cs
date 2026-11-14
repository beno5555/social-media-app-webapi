using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace aspnetproject.IntegrationTests.Serialization;

// Every time the API writes, in a response or a hub push, ends in Z (ADR 0005). Times are read as raw JSON text,
// because a typed read would hide a missing suffix.
[Collection("Database")]
public class UtcTimeSerializationTests : IAsyncLifetime
{
    private const string DataProperty               = "data";
    private const string IdProperty                 = "id";
    private const string SentAtProperty             = "sentAt";
    private const string SeenAtProperty             = "seenAt";
    private const string ConversationFriendProperty = "conversationFriend";
    private const string LastMessageSentAtProperty  = "lastMessageSentAt";
    private const string DateOfBirthProperty        = "dateOfBirth";
    private const string UtcSuffix                  = "Z";

    // Message 5 (alice → bob) is seeded with these fixed UTC times.
    private const string SeededMessageSentAt = "2026-09-01T10:00:00Z";
    private const string SeededMessageSeenAt = "2026-09-01T10:05:00Z";

    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public UtcTimeSerializationTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new CustomWebApplicationFactory();
        _client    = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- GET /messages/conversation/{otherUserId}: times read back from the database ----------

    [Fact]
    public async Task GetConversation_SeededMessage_WritesStoredTimesAsUtc()
    {
        await SignInAsync("alice");

        var body    = await GetJsonAsync("api/messages/conversation/3"); // with bob
        var message = body.GetProperty(DataProperty).EnumerateArray().Single(message => message.GetProperty(IdProperty).GetInt32() == 5);

        Assert.Equal(SeededMessageSentAt, message.GetProperty(SentAtProperty).GetString());
        Assert.Equal(SeededMessageSeenAt, message.GetProperty(SeenAtProperty).GetString());
    }

    // ---------- GET /messages/friends/conversation: read with raw SQL into a projection, past any EF Core mapping ----------

    [Fact]
    public async Task GetConversationFriends_SeededConversation_WritesLastMessageTimeAsUtc()
    {
        await SignInAsync("alice");

        var body   = await GetJsonAsync("api/messages/friends/conversation");
        var friend = body.GetProperty(DataProperty).EnumerateArray()
            .Single(friend => friend.GetProperty(ConversationFriendProperty).GetProperty(IdProperty).GetInt32() == 3); // bob

        Assert.EndsWith(UtcSuffix, friend.GetProperty(LastMessageSentAtProperty).GetString());
    }

    // ---------- POST /messages/{receiverId}: a time created in the same request ----------

    [Fact]
    public async Task SendMessage_ToFriend_WritesSentTimeAsUtcAtTheSameInstant()
    {
        await SignInAsync("alice");
        var before = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync("api/messages/3", new CreateMessageDto { Content = "Sent just now." }); // to bob
        var after    = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body   = await ReadJsonAsync(response);
        var sentAt = body.GetProperty(DataProperty).GetProperty(SentAtProperty).GetString()!;

        Assert.EndsWith(UtcSuffix, sentAt);
        var instant = DateTimeOffset.Parse(sentAt, CultureInfo.InvariantCulture).UtcDateTime;
        Assert.InRange(instant, before, after);
    }

    // ---------- Hub payloads ----------
    // Every time a service pushes today is created in the same request, so it already carries the suffix. The push below
    // goes through the real hub context and protocol with a time that has no kind, as a time read back from the database would.

    [Fact]
    public async Task HubPush_TimeWithoutKind_WritesTimeAsUtc()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        var hubContext      = _factory.Services.GetRequiredService<IHubContext<MessageHub>>();

        await hubContext.Clients.Group("3").SendAsync(HubEvents.ReceiveMessage, new PushMessageDto // to bob
        {
            Id      = 5,
            Content = "Old message from Alice to Bob, past the edit window.",
            Sender  = new MinimalUserDto { Id = 2, Username = "alice" },
            SentAt  = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Unspecified)
        });
        var pushed = await bob.WaitForEventAsync<JsonElement>(HubEvents.ReceiveMessage);

        Assert.Equal(SeededMessageSentAt, pushed.GetProperty(SentAtProperty).GetString());
    }

    // ---------- PUT /users/mine: times the API receives ----------
    // A bare date and a browser's toISOString() both name the same calendar date.

    [Theory]
    [InlineData("2000-08-16")]
    [InlineData("2000-08-16T00:00:00.000Z")]
    public async Task UpdateMine_DateOfBirth_ReadsBackAsTheSameCalendarDate(string sentDateOfBirth)
    {
        await SignInAsync("alice");

        var json     = $$"""{ "username": "alice", "dateOfBirth": "{{sentDateOfBirth}}", "bio": "unchanged" }""";
        var response = await _client.PutAsync("api/users/mine", new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await GetJsonAsync("api/users/mine");

        Assert.Equal("2000-08-16T00:00:00Z", body.GetProperty(DataProperty).GetProperty(DateOfBirthProperty).GetString());
    }

    private async Task SignInAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}

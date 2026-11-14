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

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Bob, this one arrives live.")); // to bob
        var pushed = await bob.WaitForEventAsync<PushMessageDto>(HubEvents.ReceiveMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<SentMessageDto>>();
        Assert.Equal(body!.Data!.Id, pushed.Id);
        Assert.Equal("Bob, this one arrives live.", pushed.Content);
        Assert.Equal(2, pushed.Sender.Id); // alice
        Assert.Equal("alice", pushed.Sender.Username);
    }

    [Fact]
    public async Task SendMessage_WithPhotos_DeliversPhotosInOrderWithServedAddresses()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
        var png  = new PostFormFile(TestImages.Png(1), "first.png", TestImages.PngContentType);
        var jpeg = new PostFormFile(TestImages.Jpeg(2), "second.jpg", TestImages.JpegContentType);

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create(null, png, jpeg)); // to bob
        var pushed   = await bob.WaitForEventAsync<PushMessageDto>(HubEvents.ReceiveMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal([0, 1], pushed.Images.Select(image => image.OrderIndex));
        Assert.Equal(TestImages.Png(1), await _client.GetByteArrayAsync(pushed.Images[0].Address));
        Assert.Equal(TestImages.Jpeg(2), await _client.GetByteArrayAsync(pushed.Images[1].Address));
    }

    [Fact]
    public async Task SendMessage_ToFriend_DeliversNothingToThirdUser()
    {
        await using var bob     = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var charlie = await HubTestClient.ConnectAsync(_factory, "charlie");
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Only Bob should see this.")); // to bob
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

    // ---------- POST /posts/{postId}/likes → PostLiked ----------
    // Post 2 is bob's (3). alice (2), charlie (4) and erin (6) already like it; diana (5) does not.

    [Fact]
    public async Task LikePost_BySomeoneElse_DeliversPostLikedToAuthor()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts/2/likes", null);
        var pushed   = await bob.WaitForEventAsync<PostLikedDto>(HubEvents.PostLiked);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, pushed.PostId);
        Assert.Equal(5, pushed.LikedBy.Id); // diana
        Assert.Equal("diana", pushed.LikedBy.Username);
        Assert.Equal(4, pushed.LikeCount);
    }

    [Fact]
    public async Task LikePost_BySomeoneElse_DeliversNothingToLikerOrThirdUser()
    {
        await using var bob   = await HubTestClient.ConnectAsync(_factory, "bob");
        await using var alice = await HubTestClient.ConnectAsync(_factory, "alice");
        await using var diana = await HubTestClient.ConnectAsync(_factory, "diana");
        await LoginAsync("diana");

        await _client.PostAsync("api/posts/2/likes", null);
        await bob.WaitForEventAsync<PostLikedDto>(HubEvents.PostLiked);

        await alice.AssertNoEventAsync(HubEvents.PostLiked);
        await diana.AssertNoEventAsync(HubEvents.PostLiked);
    }

    [Fact]
    public async Task LikePost_OwnPost_DeliversNothing()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        await LoginAsync("bob");

        var response = await _client.PostAsync("api/posts/2/likes", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await bob.AssertNoEventAsync(HubEvents.PostLiked);
    }

    [Fact]
    public async Task LikePost_RepeatedLike_DeliversNothing()
    {
        await using var bob = await HubTestClient.ConnectAsync(_factory, "bob");
        await LoginAsync("alice");

        var response = await _client.PostAsync("api/posts/2/likes", null); // alice already likes it

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await bob.AssertNoEventAsync(HubEvents.PostLiked);
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

    private async Task LoginAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
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

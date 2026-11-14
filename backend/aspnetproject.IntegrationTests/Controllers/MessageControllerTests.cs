using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Users.Friends;
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

    // ---------- POST /messages/{receiverId} ----------
    // multipart/form-data: Content, Images (up to 5). A message needs text or at least one photo.
    // alice (2) and bob (3) are accepted friends. charlie (4) is friends with neither.

    [Fact]
    public async Task SendMessage_WithTextAndPhotos_ReturnsPhotosInUploadOrder()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Look at these", ImageFormFiles.Png(1), ImageFormFiles.Jpeg(2), ImageFormFiles.WebP(3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await ResponseReader.ReadDataAsync<SentMessageDto>(response);
        Assert.Equal("Look at these", message.Content);
        Assert.Equal([0, 1, 2], message.Images.Select(image => image.OrderIndex));
        Assert.EndsWith(".png", message.Images[0].Address);
        Assert.EndsWith(".jpg", message.Images[1].Address);
        Assert.EndsWith(".webp", message.Images[2].Address);
        Assert.All(message.Images, image => Assert.StartsWith("/api/images/", image.Address));
    }

    [Fact]
    public async Task SendMessage_WithPhotosOnly_ReturnsCreatedWithEmptyContent()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create(null, ImageFormFiles.Jpeg(1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await ResponseReader.ReadDataAsync<SentMessageDto>(response);
        Assert.Equal(string.Empty, message.Content);
        Assert.Single(message.Images);
    }

    [Fact]
    public async Task SendMessage_WithTextOnly_ReturnsCreatedWithNoPhotos()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Just words"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await ResponseReader.ReadDataAsync<SentMessageDto>(response);
        Assert.Equal("Just words", message.Content);
        Assert.Empty(message.Images);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessage_WithNeitherTextNorPhotos_ReturnsBadRequestWithTextOrImageRequired(string content)
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create(content));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.MessageTextOrImageRequired, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task SendMessage_WithWrongFormatFile_ReturnsBadRequestAndSavesNothing()
    {
        await LoginAsAsync("alice");
        var disguisedScript = new ImageFormFile(TestImages.NotAnImage(), "photo.jpg", TestImages.JpegContentType);

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Half good", ImageFormFiles.Png(1), disguisedScript));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.ImageFormatNotAccepted, await ResponseReader.ReadMessageAsync(response));
        await AssertNothingSavedAsync(_client, _factory, "Half good");
    }

    [Fact]
    public async Task SendMessage_WithPhotoOverSizeLimit_ReturnsBadRequestAndSavesNothing()
    {
        // The limit is lowered through configuration, so the test does not upload megabytes.
        await using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ImageStorage:MaxFileSizeBytes"] = "1024"
        });
        using var client = factory.CreateClient();
        AuthTestHelper.AttachToken(client, await AuthTestHelper.LoginAsync(client, "alice", TestDataSeeder.SharedPlaintextPassword));
        var oversizedPhoto = new ImageFormFile(TestImages.JpegOfSize(1025), "big.jpg", TestImages.JpegContentType);

        var response = await client.PostAsync("api/messages/3", MessageForm.Create("Too big", ImageFormFiles.Png(1), oversizedPhoto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each image must be 0.001 MB or smaller", await ResponseReader.ReadMessageAsync(response));
        await AssertNothingSavedAsync(client, factory, "Too big");
    }

    [Fact]
    public async Task SendMessage_WithSixPhotos_ReturnsBadRequestAndSavesNothing()
    {
        await LoginAsAsync("alice");
        var photos = Enumerable.Range(1, 6).Select(marker => ImageFormFiles.Jpeg((byte)marker)).ToArray();

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Too many", photos));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A message can have at most 5 images", await ResponseReader.ReadMessageAsync(response));
        await AssertNothingSavedAsync(_client, _factory, "Too many");
    }

    // Five photos at the default 5 MB limit: the request size limit on sending must fit them.
    [Fact]
    public async Task SendMessage_WithFivePhotosAtSizeLimit_ReturnsCreated()
    {
        await LoginAsAsync("alice");
        var photos = Enumerable.Range(1, 5)
            .Select(_ => new ImageFormFile(TestImages.JpegOfSize(5 * Constants.BytesPerMegabyte), "big.jpg", TestImages.JpegContentType))
            .ToArray();

        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Five big ones", photos));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(5, (await ResponseReader.ReadDataAsync<SentMessageDto>(response)).Images.Count);
    }

    [Fact]
    public async Task SendMessage_ToNonFriend_ReturnsBadRequestWithFriendsOnly()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/4", MessageForm.Create("Hi Charlie", ImageFormFiles.Jpeg(1))); // charlie

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.CanOnlySendMessagesToFriends, await ResponseReader.ReadMessageAsync(response));
        Assert.Empty(Directory.EnumerateFiles(_factory.UploadFolder));
    }

    [Fact]
    public async Task SendMessage_ToOneself_ReturnsBadRequestWithCannotMessageOneself()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/2", MessageForm.Create("Note to self"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.CannotSendMessagesToOneself, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task SendMessage_ToUnknownUser_ReturnsBadRequestWithReceiverNotFound()
    {
        await LoginAsAsync("alice");

        var response = await _client.PostAsync("api/messages/9999", MessageForm.Create("Anyone there?"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.ReceiverUserNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task SendMessage_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("api/messages/3", MessageForm.Create("Anonymous message."));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- GET /messages/conversation/{otherUserId} ----------

    [Fact]
    public async Task GetConversation_MessageWithPhotos_ReturnsPhotosInOrder()
    {
        await LoginAsAsync("alice");
        var sent = await ResponseReader.ReadDataAsync<SentMessageDto>(
            await _client.PostAsync("api/messages/3", MessageForm.Create("Holiday", ImageFormFiles.WebP(7), ImageFormFiles.Png(8))));
        await LoginAsAsync("bob");

        var messages = await ResponseReader.ReadListAsync<StandardMessageDto>(await _client.GetAsync("api/messages/conversation/2"));

        var message = messages.Single(message => message.Id == sent.Id);
        Assert.Equal([0, 1], message.Images.Select(image => image.OrderIndex));
        Assert.EndsWith(".webp", message.Images[0].Address);
        Assert.EndsWith(".png", message.Images[1].Address);
        Assert.All(messages.Where(other => other.Id != sent.Id), other => Assert.Empty(other.Images));
    }

    // ---------- GET /messages/friends/conversation ----------
    // bob (3) has one conversation, with alice (2). Its seeded messages carry no photos.

    [Fact]
    public async Task GetConversationFriends_LastMessageIsPhotoOnly_ReturnsItsImageCount()
    {
        await LoginAsAsync("alice");
        await _client.PostAsync("api/messages/3", MessageForm.Create(null, ImageFormFiles.Jpeg(1), ImageFormFiles.Png(2)));
        await LoginAsAsync("bob");

        var entries = await ResponseReader.ReadListAsync<ConversationFriendDto>(await _client.GetAsync("api/messages/friends/conversation"));

        var withAlice = entries.Single(entry => entry.ConversationFriend.Id == 2);
        Assert.Equal(string.Empty, withAlice.LastMessageContent);
        Assert.Equal(2, withAlice.LastMessageImageCount);
    }

    [Fact]
    public async Task GetConversationFriends_LastMessageHasNoPhotos_ReturnsZeroImageCount()
    {
        await LoginAsAsync("alice");
        await _client.PostAsync("api/messages/3", MessageForm.Create("Photos first", ImageFormFiles.Jpeg(1)));
        await _client.PostAsync("api/messages/3", MessageForm.Create("Then words"));
        await LoginAsAsync("bob");

        var entries = await ResponseReader.ReadListAsync<ConversationFriendDto>(await _client.GetAsync("api/messages/friends/conversation"));

        var withAlice = entries.Single(entry => entry.ConversationFriend.Id == 2);
        Assert.Equal("Then words", withAlice.LastMessageContent);
        Assert.Equal(0, withAlice.LastMessageImageCount);
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
        Assert.Equal(ResponseMessages.EditWindowExpired, await ResponseReader.ReadMessageAsync(response));
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
        Assert.Equal(ResponseMessages.DoNotHavePermissionToEditMessage, await ResponseReader.ReadMessageAsync(response));
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
        Assert.Equal(ResponseMessages.InvalidRequest, await ResponseReader.ReadMessageAsync(response));
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
        Assert.Equal(ResponseMessages.MessageNotFound, await ResponseReader.ReadMessageAsync(response));
    }

    // ---------- DELETE /messages/{id} ----------

    [Fact]
    public async Task DeleteMessage_WithPhotos_RemovesMessageAndItsFiles()
    {
        await LoginAsAsync("alice");
        var sent = await ResponseReader.ReadDataAsync<SentMessageDto>(
            await _client.PostAsync("api/messages/3", MessageForm.Create("Delete me", ImageFormFiles.Jpeg(1), ImageFormFiles.Png(2))));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(sent.Images[0].Address)).StatusCode);

        var response = await _client.DeleteAsync($"api/messages/{sent.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var image in sent.Images)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(image.Address)).StatusCode);
        }
        Assert.Empty(Directory.EnumerateFiles(_factory.UploadFolder));
        var messages = await ResponseReader.ReadListAsync<StandardMessageDto>(await _client.GetAsync("api/messages/conversation/3"));
        Assert.DoesNotContain(messages, message => message.Id == sent.Id);
    }

    // ---------- helpers ----------

    // The refused message is not in alice's conversation with bob, and no file reached the upload folder.
    private static async Task AssertNothingSavedAsync(HttpClient client, CustomWebApplicationFactory factory, string content)
    {
        var messages = await ResponseReader.ReadListAsync<StandardMessageDto>(await client.GetAsync("api/messages/conversation/3"));
        Assert.DoesNotContain(messages, message => message.Content == content);
        Assert.Empty(Directory.EnumerateFiles(factory.UploadFolder));
    }
}

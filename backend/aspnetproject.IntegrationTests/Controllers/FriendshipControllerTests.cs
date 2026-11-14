using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class FriendshipControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture             _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public FriendshipControllerTests(DatabaseFixture dbFixture)
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

    // ---------- DELETE /friendships/{friendId} ----------
    // alice (2) and bob (3) are accepted friends with a seeded conversation. Removing the friendship deletes it.

    [Fact]
    public async Task RemoveRelationship_ConversationWithPhotos_RemovesTheirFiles()
    {
        var fromAlice = await SendPhotoMessageAsync("alice", receiverId: 3, TestImages.Jpeg(1));
        var fromBob   = await SendPhotoMessageAsync("bob", receiverId: 2, TestImages.Png(2));

        var response = await _client.DeleteAsync("api/friendships/2"); // bob removes alice

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var address in fromAlice.Images.Concat(fromBob.Images).Select(image => image.Address))
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(address)).StatusCode);
        }
        Assert.Empty(Directory.EnumerateFiles(_factory.UploadFolder));
    }

    // ---------- helpers ----------

    // Signs in as the sender, who stays signed in, and sends one photo.
    private async Task<SentMessageDto> SendPhotoMessageAsync(string sender, int receiverId, byte[] photo)
    {
        var token = await AuthTestHelper.LoginAsync(_client, sender, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var file     = new PostFormFile(photo, "photo", TestImages.JpegContentType);
        var response = await _client.PostAsync($"api/messages/{receiverId}", MessageForm.Create(null, file));
        var body     = await response.Content.ReadFromJsonAsync<ApplicationResponse<SentMessageDto>>();

        return body!.Data!;
    }
}

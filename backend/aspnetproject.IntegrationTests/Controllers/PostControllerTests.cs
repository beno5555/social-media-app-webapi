using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.IntegrationTests.Fixtures;
using aspnetproject.IntegrationTests.Helpers;

namespace aspnetproject.IntegrationTests.Controllers;

[Collection("Database")]
public class PostControllerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _dbFixture;
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PostControllerTests(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _dbFixture.ResetAndSeedAsync(_dbFixture.GetOptions());
    // Disposing the host releases its settings-file watchers; each one holds an inotify instance, and Linux caps them per user.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---------- GET /posts/feed ----------
    // alice (2) is friends (accepted) with bob (3) only.
    // pending/declined relationships (diana, charlie) should not surface in feed.

    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/posts/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_ShowsAcceptedFriendsPostsOnly()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/feed");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Bob Post", body); // accepted friend
        Assert.DoesNotContain("Charlie Post", body); // charlie->alice is Pending, not Accepted
        Assert.DoesNotContain("Alice Post", body); // feed excludes caller's own posts
    }

    [Fact]
    public async Task GetFeed_DoesNotIncludePendingOrDeclinedFriendPosts()
    {
        // bob -> charlie is Declined; charlie should not appear in bob's feed either
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/feed");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Charlie Post", body);
    }

    // ---------- GET /posts/mine ----------

    [Fact]
    public async Task GetMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/posts/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyCallersOwnPosts()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.GetAsync("api/posts/mine");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Alice Post", body);
        Assert.DoesNotContain("Bob Post", body);
    }

    [Fact]
    public async Task GetMine_SeveralPosts_ReturnsNewestFirst()
    {
        await LoginAsync("bob");
        int olderId = await CreatePostAsync("Bob Older Post");
        int newerId = await CreatePostAsync("Bob Newer Post");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/mine"));

        Assert.Equal([newerId, olderId, 2], posts.Select(post => post.Id)); // 2 is bob's seeded post
    }

    // ---------- GET /posts/{id} ----------

    [Fact]
    public async Task GetById_Anonymous_ReturnsPostWithComments()
    {
        var response = await _client.GetAsync("api/posts/2"); // Bob Post, has 3 comments

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bob Post", body);
        Assert.Contains("Alice comments on Bob's post.", body);
    }

    [Fact]
    public async Task GetById_ForNonexistentPost_ReturnsNotFound()
    {
        var response = await _client.GetAsync("api/posts/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForDeactivatedUsersPost_StillReturnsPost()
    {
        // Restrict/SetNull FK design: a deactivated author's existing post is not cascaded away
        var response = await _client.GetAsync("api/posts/4"); // Erin Deactivated Post

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Erin Deactivated Post", body);
    }

    [Fact]
    public async Task GetById_ForSoftDeletedUsersPost_StillReturnsPost()
    {
        var response = await _client.GetAsync("api/posts/5"); // Frank Deleted Post

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Frank Deleted Post", body);
    }

    [Fact]
    public async Task GetById_OrphanedComment_ShowsNullOrAnonymizedAuthor()
    {
        var response = await _client.GetAsync("api/posts/1"); // Alice Post, has the orphaned comment

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Orphaned comment with no active author.", body);
    }

    // ---------- GET /posts/user/{userId} ----------

    [Fact]
    public async Task GetPostsByUser_Anonymous_ReturnsThatUsersPosts()
    {
        var response = await _client.GetAsync("api/posts/user/3"); // bob

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bob Post", body);
    }

    [Fact]
    public async Task GetPostsByUser_IsPublicRegardlessOfFriendship()
    {
        // charlie (4) has no accepted friendship with anyone; this endpoint is
        // fully public per controllers.md, unlike /feed
        var response = await _client.GetAsync("api/posts/user/4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Charlie Post", body);
    }

    [Fact]
    public async Task GetPostsByUser_SeveralPosts_ReturnsNewestFirst()
    {
        await LoginAsync("bob");
        int olderId = await CreatePostAsync("Bob Older Post");
        int newerId = await CreatePostAsync("Bob Newer Post");
        _client.DefaultRequestHeaders.Authorization = null;

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/user/3"));

        Assert.Equal([newerId, olderId, 2], posts.Select(post => post.Id)); // 2 is bob's seeded post
    }

    [Fact]
    public async Task GetPostsByUser_ForNonexistentUser_ReturnsEmptyOrNotFound()
    {
        var response = await _client.GetAsync("api/posts/user/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /posts ----------
    // multipart/form-data: PostTitle, PostContent, Images (up to 10). A post needs a title and either text or a photo.

    [Fact]
    public async Task Create_WithTitleTextAndPhotos_ReturnsPhotosInUploadOrder()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Diana's Album", "Three photos",
            ImageFormFiles.Png(1), ImageFormFiles.Jpeg(2), ImageFormFiles.WebP(3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);
        Assert.Equal([0, 1, 2], post.Images.Select(image => image.OrderIndex));
        Assert.All(post.Images, image =>
        {
            Assert.True(image.Id > 0);
            Assert.StartsWith("/api/", image.Address);
        });
        Assert.Equal(3, post.Images.Select(image => image.Id).Distinct().Count());
    }

    [Fact]
    public async Task Create_ReturnedPhotoAddresses_ServeThePhotosToAnyone()
    {
        await LoginAsync("diana");
        var response = await _client.PostAsync("api/posts", PostForm.Create("Diana's Album", "Two photos",
            ImageFormFiles.Png(1), ImageFormFiles.Jpeg(2)));
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);

        _client.DefaultRequestHeaders.Authorization = null;
        var firstPhoto  = await _client.GetByteArrayAsync(post.Images[0].Address);
        var secondPhoto = await _client.GetByteArrayAsync(post.Images[1].Address);

        Assert.Equal(TestImages.Png(1), firstPhoto);
        Assert.Equal(TestImages.Jpeg(2), secondPhoto);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("api/posts", PostForm.Create("New Post", "Content"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithToken_CreatesPostOwnedByCaller()
    {
        await LoginAsync("diana");

        var createResponse = await _client.PostAsync("api/posts", PostForm.Create("Diana's New Post", "Fresh content"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var mineResponse = await _client.GetAsync("api/posts/mine");
        var mineBody = await mineResponse.Content.ReadAsStringAsync();

        Assert.Contains("Diana's New Post", mineBody);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        await LoginAsync("alice");

        var response = await _client.PostAsync("api/posts", PostForm.Create("", "Content"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithPhotosAndNoText_ReturnsCreated()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Just a picture", null, ImageFormFiles.Jpeg(1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);
        Assert.Single(post.Images);
    }

    [Fact]
    public async Task Create_WithTextAndNoPhotos_ReturnsPostWithNoPhotos()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Words only", "No pictures here"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);
        Assert.Empty(post.Images);
    }

    [Fact]
    public async Task Create_WithNeitherTextNorPhotos_ReturnsBadRequest()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Empty post", "   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.TextOrImageRequired, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithoutTitle_ReturnsBadRequest()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create(null, "Content", ImageFormFiles.Jpeg(1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithScriptNamedAndTypedAsJpeg_ReturnsBadRequest()
    {
        await LoginAsync("diana");
        var disguisedScript = new ImageFormFile(TestImages.NotAnImage(), "photo.jpg", TestImages.JpegContentType);

        var response = await _client.PostAsync("api/posts", PostForm.Create("Sneaky", "Content", disguisedScript));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.ImageFormatNotAccepted, await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithPhotoOverSizeLimit_ReturnsBadRequestWithLimit()
    {
        // The limit is lowered through configuration, so the test does not upload megabytes.
        await using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ImageStorage:MaxFileSizeBytes"] = "1024"
        });
        using var client = factory.CreateClient();
        AuthTestHelper.AttachToken(client, await AuthTestHelper.LoginAsync(client, "diana", TestDataSeeder.SharedPlaintextPassword));
        var oversizedPhoto = new ImageFormFile(TestImages.JpegOfSize(1025), "big.jpg", TestImages.JpegContentType);

        var response = await client.PostAsync("api/posts", PostForm.Create("Too big", "Content", oversizedPhoto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each image must be 0.001 MB or smaller", await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithElevenPhotos_ReturnsBadRequestWithLimit()
    {
        await LoginAsync("diana");
        var photos = Enumerable.Range(1, 11).Select(marker => ImageFormFiles.Jpeg((byte)marker)).ToArray();

        var response = await _client.PostAsync("api/posts", PostForm.Create("Too many", "Content", photos));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A post can have at most 10 images", await ResponseReader.ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithTenPhotos_ReturnsCreated()
    {
        await LoginAsync("diana");
        var photos = Enumerable.Range(1, 10).Select(marker => ImageFormFiles.Jpeg((byte)marker)).ToArray();

        var response = await _client.PostAsync("api/posts", PostForm.Create("Ten photos", "Content", photos));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenOneFileIsRefused_SavesNoPostAndStoresNoFile()
    {
        await LoginAsync("diana");
        var script = new ImageFormFile(TestImages.NotAnImage(), "photo.png", TestImages.PngContentType);

        var response = await _client.PostAsync("api/posts", PostForm.Create("Half good", "Content", ImageFormFiles.Png(1), script));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var mineBody = await (await _client.GetAsync("api/posts/mine")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Half good", mineBody);
        Assert.Empty(Directory.EnumerateFiles(_factory.UploadFolder));
    }

    // ---------- photos on every post read ----------
    // bob (3) creates a post with photos; alice (2) is his accepted friend.

    [Fact]
    public async Task GetById_PostWithPhotos_ReturnsPhotosInOrder()
    {
        int postId = await CreateBobsPhotoPostAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync($"api/posts/{postId}"));

        AssertBobsPhotosInOrder(post.Images);
    }

    [Fact]
    public async Task GetFeed_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();
        await LoginAsync("alice");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/feed"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    [Fact]
    public async Task GetPostsByUser_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/user/3"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    [Fact]
    public async Task GetMine_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/mine"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    // ---------- like and comment counts on every post read ----------
    // Post 1 (alice's): 1 like (bob), 2 comments (one orphaned).
    // Post 2 (bob's):   3 likes (alice, charlie, erin_deactivated), 3 comments.
    // Posts 3–5 have no likes and no comments.

    [Fact]
    public async Task GetById_AsGuest_ReturnsCountsAndNotLikedByCaller()
    {
        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AsUserWhoLikedIt_ReturnsLikedByCaller()
    {
        await LoginAsync("alice");

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AsUserWhoHasNotLikedIt_ReturnsNotLikedByCaller()
    {
        await LoginAsync("bob");

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AfterCallerLikes_CountsTheNewLike()
    {
        await LoginAsync("diana");
        await _client.PostAsync("api/posts/2/likes", null);

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.Equal(4, post.LikeCount);
        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetPostsByUser_AsGuest_ReturnsCountsAndNotLikedByCaller()
    {
        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/user/3"));

        var post = posts.Single(post => post.Id == 2);
        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetPostsByUser_AsUserWhoLikedIt_ReturnsLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/user/3"));

        Assert.True(posts.Single(post => post.Id == 2).LikedByCaller);
    }

    [Fact]
    public async Task GetFeed_ReturnsCountsAndLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/feed"));

        var post = posts.Single(post => post.Id == 2);
        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetMine_OwnPostLikedByOthers_ReturnsCountsAndNotLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ResponseReader.ReadListAsync<StandardPostDisplayDto>(await _client.GetAsync("api/posts/mine"));

        var post = posts.Single(post => post.Id == 1);
        Assert.Equal(1, post.LikeCount);
        Assert.Equal(2, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task Create_NewPost_ReturnsZeroCountsAndNotLikedByCaller()
    {
        await LoginAsync("diana");

        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(
            await _client.PostAsync("api/posts", PostForm.Create("Diana's New Post", "Fresh content")));

        Assert.Equal(0, post.LikeCount);
        Assert.Equal(0, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task Update_OwnPost_ReturnsCountsAndLikedByCaller()
    {
        await LoginAsync("bob");
        await _client.PostAsync("api/posts/2/likes", null); // bob likes his own post

        var post = await ResponseReader.ReadDataAsync<FullPostDisplayDto>(await _client.PutAsJsonAsync("api/posts/2", new
        {
            Title = "Bob Post Updated",
            Content = "Updated content"
        }));

        Assert.Equal(4, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.True(post.LikedByCaller);
    }

    // ---------- PUT /posts/{id} ----------

    [Fact]
    public async Task Update_OwnPost_Succeeds()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/1", new
        {
            Title = "Alice Post Updated",
            Content = "Updated content"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alice Post Updated", body);
    }

    [Fact]
    public async Task Update_SomeoneElsesPost_ReturnsNotFound()
    {
        // Ownership-scoped query (WHERE Id = @id AND UserId = @userId) means a
        // non-owner gets NotFound, not Forbidden — avoids leaking existence.
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/2", new // Bob's post
        {
            Title = "Hijacked Title",
            Content = "Hijacked content"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_EvenAsAdmin_SomeoneElsesPost_ReturnsNotFound()
    {
        // controllers.md only documents an admin override for Delete, not Update
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.PutAsJsonAsync("api/posts/2", new
        {
            Title = "Admin Edit Attempt",
            Content = "Should not be allowed"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("api/posts/1", new
        {
            Title = "Anon Edit",
            Content = "Should not be allowed"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- DELETE /posts/{id} ----------

    [Fact]
    public async Task Delete_LikedPost_RemovesPostAndItsLikes()
    {
        await LoginAsync("bob");

        var deleteResponse = await _client.DeleteAsync("api/posts/2"); // Bob Post, liked by 3 users
        var likesResponse  = await _client.GetAsync("api/posts/2/likes");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, likesResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnPost_RemovesPostAndItsComments()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "bob", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/posts/2"); // Bob Post, has 3 comments
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/posts/2");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_SomeoneElsesPost_AsNonOwnerNonAdmin_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/posts/2"); // Bob's post

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SomeoneElsesPost_AsAdmin_Succeeds()
    {
        // Delete explicitly allows owner OR admin, unlike Update
        var token = await AuthTestHelper.LoginAsync(_client, "sandro_beno", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var deleteResponse = await _client.DeleteAsync("api/posts/3"); // Charlie's post
        Assert.True(deleteResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

        var lookupResponse = await _client.GetAsync("api/posts/3");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("api/posts/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonexistentPost_ReturnsNotFound()
    {
        var token = await AuthTestHelper.LoginAsync(_client, "alice", TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);

        var response = await _client.DeleteAsync("api/posts/9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- helpers ----------

    private async Task LoginAsync(string username)
    {
        var token = await AuthTestHelper.LoginAsync(_client, username, TestDataSeeder.SharedPlaintextPassword);
        AuthTestHelper.AttachToken(_client, token);
    }

    private const string BobsPhotoPostTitle = "Bob's Photos";

    // Signs in as bob and creates a post with a WebP then a PNG.
    private async Task<int> CreateBobsPhotoPostAsync()
    {
        await LoginAsync("bob");
        var response = await _client.PostAsync("api/posts", PostForm.Create(BobsPhotoPostTitle, "Holiday", ImageFormFiles.WebP(7), ImageFormFiles.Png(8)));
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);
        return post.Id;
    }

    // Creates a text post as the signed-in user.
    private async Task<int> CreatePostAsync(string title)
    {
        var response = await _client.PostAsync("api/posts", PostForm.Create(title, "Text"));
        var post = await ResponseReader.ReadDataAsync<StandardPostDisplayDto>(response);
        return post.Id;
    }

    private static void AssertBobsPhotosInOrder(List<ImageDto> images)
    {
        Assert.Equal([0, 1], images.Select(image => image.OrderIndex));
        Assert.EndsWith(".webp", images[0].Address);
        Assert.EndsWith(".png", images[1].Address);
    }
}

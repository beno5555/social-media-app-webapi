using System.Net;
using System.Net.Http.Json;
using aspnetproject.Common.Domain;
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
    // alice (2) is friends (accepted) with bob (3). bob is also friends with diana (5), so diana is a
    // mutual-friend user for alice even though their own request is pending. charlie (4) is a stranger to alice.
    // Posts 1 to 5 are created at seed time, so they are the newest. Posts 6 to 13 have fixed past times
    // and come in groups where two ranking factors are equal (see TestDataSeeder):
    //   connection: 6 (bob), 7 (diana), 8 (charlie), all at 09:00 with no activity
    //   activity:   9 (one like, one comment) and 10 (none), charlie's at 08:00
    //   age:        11 (07:00) and 12 (06:00), charlie's with no activity
    //   tie:        13, the same score as 10
    // Posts 4 (erin, deactivated) and 5 (frank, soft-deleted) must never appear.

    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/posts/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_IncludesFriendsAndOtherUsersPosts()
    {
        await LoginAsync("alice");

        var ids = await ReadFeedIdsAsync();

        Assert.Contains(2, ids); // bob, a friend
        Assert.Contains(3, ids); // charlie, not a friend
    }

    [Fact]
    public async Task GetFeed_EqualAgeAndActivity_RanksFriendThenMutualFriendThenStranger()
    {
        await LoginAsync("alice");

        var ids = await ReadFeedIdsAsync();

        Assert.Equal([6, 7, 8], ids.Where(id => id is 6 or 7 or 8));
    }

    [Fact]
    public async Task GetFeed_EqualConnectionAndAge_RanksMoreActivePostFirst()
    {
        await LoginAsync("alice");

        var ids = await ReadFeedIdsAsync();

        Assert.Equal([9, 10], ids.Where(id => id is 9 or 10));
    }

    [Fact]
    public async Task GetFeed_EqualConnectionAndActivity_RanksNewerPostFirst()
    {
        await LoginAsync("alice");

        var ids = await ReadFeedIdsAsync();

        Assert.Equal([11, 12], ids.Where(id => id is 11 or 12));
    }

    [Fact]
    public async Task GetFeed_EqualScores_ReturnsNewestIdFirstOnEveryRequest()
    {
        await LoginAsync("alice");

        var first  = await ReadFeedIdsAsync();
        var second = await ReadFeedIdsAsync();

        Assert.Equal([13, 10], first.Where(id => id is 10 or 13));
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetFeed_UserWithNoFriends_ReturnsPosts()
    {
        await LoginAsync("charlie"); // charlie's only relationships are pending or declined

        var ids = await ReadFeedIdsAsync();

        Assert.NotEmpty(ids);
    }

    [Fact]
    public async Task GetFeed_NeverIncludesCallersOwnPosts()
    {
        await LoginAsync("charlie");

        var ids = await ReadFeedIdsAsync();

        Assert.DoesNotContain(ids, id => id is 3 or >= 8 and <= 13); // every post charlie (4) owns
    }

    [Fact]
    public async Task GetFeed_PostsByDeactivatedOrSoftDeletedAccounts_AreLeftOut()
    {
        await LoginAsync("alice");

        var ids = await ReadFeedIdsAsync();

        Assert.DoesNotContain(4, ids); // erin, deactivated
        Assert.DoesNotContain(5, ids); // frank, soft-deleted
    }

    [Fact]
    public async Task GetFeed_TwoPages_ContinueTheRankedOrder()
    {
        await LoginAsync("alice");
        var allIds = await ReadFeedIdsAsync();

        var firstPage  = await ReadListAsync(await _client.GetAsync("api/posts/feed?pageNumber=1&pageSize=4"));
        var secondPage = await ReadListAsync(await _client.GetAsync("api/posts/feed?pageNumber=2&pageSize=4"));

        Assert.Equal(allIds.Take(8), firstPage.Concat(secondPage).Select(post => post.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetFeed_PageNumberBelowOne_ReturnsBadRequest(int pageNumber)
    {
        await LoginAsync("alice");

        var response = await _client.GetAsync($"api/posts/feed?pageNumber={pageNumber}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/mine"));

        // 2 and 6 are bob's seeded posts; 2 is created at seed time, 6 at a fixed past time.
        Assert.Equal([newerId, olderId, 2, 6], posts.Select(post => post.Id));
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

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/user/3"));

        // 2 and 6 are bob's seeded posts; 2 is created at seed time, 6 at a fixed past time.
        Assert.Equal([newerId, olderId, 2, 6], posts.Select(post => post.Id));
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
            Png(1), Jpeg(2), WebP(3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var post = await ReadDataAsync<StandardPostDisplayDto>(response);
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
            Png(1), Jpeg(2)));
        var post = await ReadDataAsync<StandardPostDisplayDto>(response);

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

        var response = await _client.PostAsync("api/posts", PostForm.Create("Just a picture", null, Jpeg(1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await ReadDataAsync<StandardPostDisplayDto>(response);
        Assert.Single(post.Images);
    }

    [Fact]
    public async Task Create_WithTextAndNoPhotos_ReturnsPostWithNoPhotos()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Words only", "No pictures here"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await ReadDataAsync<StandardPostDisplayDto>(response);
        Assert.Empty(post.Images);
    }

    [Fact]
    public async Task Create_WithNeitherTextNorPhotos_ReturnsBadRequest()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create("Empty post", "   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.TextOrImageRequired, await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithoutTitle_ReturnsBadRequest()
    {
        await LoginAsync("diana");

        var response = await _client.PostAsync("api/posts", PostForm.Create(null, "Content", Jpeg(1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithScriptNamedAndTypedAsJpeg_ReturnsBadRequest()
    {
        await LoginAsync("diana");
        var disguisedScript = new PostFormFile(TestImages.NotAnImage(), "photo.jpg", TestImages.JpegContentType);

        var response = await _client.PostAsync("api/posts", PostForm.Create("Sneaky", "Content", disguisedScript));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ResponseMessages.ImageFormatNotAccepted, await ReadMessageAsync(response));
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
        var oversizedPhoto = new PostFormFile(TestImages.JpegOfSize(1025), "big.jpg", TestImages.JpegContentType);

        var response = await client.PostAsync("api/posts", PostForm.Create("Too big", "Content", oversizedPhoto));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Each image must be 0.001 MB or smaller", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithElevenPhotos_ReturnsBadRequestWithLimit()
    {
        await LoginAsync("diana");
        var photos = Enumerable.Range(1, 11).Select(marker => Jpeg((byte)marker)).ToArray();

        var response = await _client.PostAsync("api/posts", PostForm.Create("Too many", "Content", photos));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A post can have at most 10 images", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Create_WithTenPhotos_ReturnsCreated()
    {
        await LoginAsync("diana");
        var photos = Enumerable.Range(1, 10).Select(marker => Jpeg((byte)marker)).ToArray();

        var response = await _client.PostAsync("api/posts", PostForm.Create("Ten photos", "Content", photos));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenOneFileIsRefused_SavesNoPostAndStoresNoFile()
    {
        await LoginAsync("diana");
        var script = new PostFormFile(TestImages.NotAnImage(), "photo.png", TestImages.PngContentType);

        var response = await _client.PostAsync("api/posts", PostForm.Create("Half good", "Content", Png(1), script));

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

        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync($"api/posts/{postId}"));

        AssertBobsPhotosInOrder(post.Images);
    }

    [Fact]
    public async Task GetFeed_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();
        await LoginAsync("alice");

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/feed"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    [Fact]
    public async Task GetPostsByUser_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/user/3"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    [Fact]
    public async Task GetMine_PostWithPhotos_ReturnsPhotosInOrder()
    {
        await CreateBobsPhotoPostAsync();

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/mine"));

        AssertBobsPhotosInOrder(posts.Single(post => post.Title == BobsPhotoPostTitle).Images);
    }

    // ---------- like and comment counts on every post read ----------
    // Post 1 (alice's): 1 like (bob), 2 comments (one orphaned).
    // Post 2 (bob's):   3 likes (alice, charlie, erin_deactivated), 3 comments.
    // Posts 3–5 have no likes and no comments.

    [Fact]
    public async Task GetById_AsGuest_ReturnsCountsAndNotLikedByCaller()
    {
        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AsUserWhoLikedIt_ReturnsLikedByCaller()
    {
        await LoginAsync("alice");

        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AsUserWhoHasNotLikedIt_ReturnsNotLikedByCaller()
    {
        await LoginAsync("bob");

        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetById_AfterCallerLikes_CountsTheNewLike()
    {
        await LoginAsync("diana");
        await _client.PostAsync("api/posts/2/likes", null);

        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.GetAsync("api/posts/2"));

        Assert.Equal(4, post.LikeCount);
        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetPostsByUser_AsGuest_ReturnsCountsAndNotLikedByCaller()
    {
        var posts = await ReadListAsync(await _client.GetAsync("api/posts/user/3"));

        var post = posts.Single(post => post.Id == 2);
        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task GetPostsByUser_AsUserWhoLikedIt_ReturnsLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/user/3"));

        Assert.True(posts.Single(post => post.Id == 2).LikedByCaller);
    }

    [Fact]
    public async Task GetFeed_ReturnsCountsAndLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/feed"));

        var post = posts.Single(post => post.Id == 2);
        Assert.Equal(3, post.LikeCount);
        Assert.Equal(3, post.CommentCount);
        Assert.True(post.LikedByCaller);
    }

    [Fact]
    public async Task GetMine_OwnPostLikedByOthers_ReturnsCountsAndNotLikedByCaller()
    {
        await LoginAsync("alice");

        var posts = await ReadListAsync(await _client.GetAsync("api/posts/mine"));

        var post = posts.Single(post => post.Id == 1);
        Assert.Equal(1, post.LikeCount);
        Assert.Equal(2, post.CommentCount);
        Assert.False(post.LikedByCaller);
    }

    [Fact]
    public async Task Create_NewPost_ReturnsZeroCountsAndNotLikedByCaller()
    {
        await LoginAsync("diana");

        var post = await ReadDataAsync<StandardPostDisplayDto>(
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

        var post = await ReadDataAsync<FullPostDisplayDto>(await _client.PutAsJsonAsync("api/posts/2", new
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
        var response = await _client.PostAsync("api/posts", PostForm.Create(BobsPhotoPostTitle, "Holiday", WebP(7), Png(8)));
        var post = await ReadDataAsync<StandardPostDisplayDto>(response);
        return post.Id;
    }

    // Creates a text post as the signed-in user.
    private async Task<int> CreatePostAsync(string title)
    {
        var response = await _client.PostAsync("api/posts", PostForm.Create(title, "Text"));
        var post = await ReadDataAsync<StandardPostDisplayDto>(response);
        return post.Id;
    }

    private static void AssertBobsPhotosInOrder(List<ImageDto> images)
    {
        Assert.Equal([0, 1], images.Select(image => image.OrderIndex));
        Assert.EndsWith(".webp", images[0].Address);
        Assert.EndsWith(".png", images[1].Address);
    }

    // One page large enough to hold every candidate, so the whole ranking is visible.
    private async Task<List<int>> ReadFeedIdsAsync()
    {
        var posts = await ReadListAsync(await _client.GetAsync($"api/posts/feed?pageSize={Constants.MaxPageSize}"));
        return posts.Select(post => post.Id).ToList();
    }

    private static async Task<List<StandardPostDisplayDto>> ReadListAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ListResponse<StandardPostDisplayDto>>();
        return body!.Data;
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        return body!.Message;
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplicationResponse<T>>();
        return body!.Data!;
    }

    private static PostFormFile Jpeg(byte marker) => new(TestImages.Jpeg(marker), "photo.jpg", TestImages.JpegContentType);
    private static PostFormFile Png(byte marker)  => new(TestImages.Png(marker), "photo.png", TestImages.PngContentType);
    private static PostFormFile WebP(byte marker) => new(TestImages.WebP(marker), "photo.webp", TestImages.WebPContentType);
}

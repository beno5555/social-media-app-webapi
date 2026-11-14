using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;
using aspnetproject.Infrastructure.Services.Storage;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class PostService : BaseService
{
    private readonly PostRepository       _postRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly UserRepository       _userRepository;
    private readonly CommentRepository    _commentRepository;
    private readonly ImageStorage         _imageStorage;

    public PostService(
        PostRepository postRepository,
        FriendshipRepository friendshipRepository,
        UserRepository userRepository,
        CommentRepository commentRepository,
        ImageStorage imageStorage,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _postRepository = postRepository;
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _imageStorage = imageStorage;
    }
    
    public async Task<ApplicationResponse<StandardPostDisplayDto>> UploadPost(int userId, CreatePostDto createPostDto)
    {
        var response   = new ApplicationResponse<StandardPostDisplayDto>();
        var newPostCheck = await CheckNewPostAsync(userId, createPostDto);

        if (newPostCheck.Succeeded)
        {
            var addedPost = await _imageStorage.SaveWithOwnerAsync(newPostCheck.Data!, imageAddresses =>
            {
                var postToAdd = PostMapper.ToEntity(userId, createPostDto, imageAddresses);
                return _postRepository.AddPostAsync(postToAdd);
            });

            var postDisplay = PostMapper.ToNewPostDisplay(addedPost);
            response.Ok(postDisplay, ResponseMessages.PostUploaded);
        }
        else
        {
            response.Fail(newPostCheck.Message);
            await LogResultAsync(response.Succeeded, nameof(UploadPost), nameof(Post), $"Could not upload post: {response.Message}", null);
        }

        return response;
    }

    // callerId is null for a guest.
    public async Task<ApplicationResponse<FullPostDisplayDto>> GetPostByIdAsync(int id, int? callerId)
    {
        var response = new ApplicationResponse<FullPostDisplayDto>();

        var post = await _postRepository.GetPostByIdAsync(id, callerId);
        if (post is not null)
        {
            var postDisplay = PostMapper.ToFullDisplay(post);
            response.Ok(postDisplay, ResponseMessages.PostRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetPostByIdAsync), nameof(Post), $"Could not retrieve post. {response.Message}", id);
        }
        return response;
    }
    
    public async Task<ListResponse<StandardPostDisplayDto>> GetFeedAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardPostDisplayDto>();
        
        var friends  = await _friendshipRepository.GetFriendshipsAsync(userId);
        List<int> friendIds = friends
            .Select(friend =>
            friend.RequesterUserId == userId ? friend.AddresseeUserId : friend.RequesterUserId)
            .ToList();

        var posts    = await _postRepository.GetFeedAsync(friendIds, userId, pageNumber, pageSize);
        var postDtos = posts.Select(PostMapper.ToStandardDisplay).ToList();

        response.Ok(postDtos, ResponseMessages.Feed);

        return response;
    }
    
    // callerId is null for a guest.
    public async Task<ListResponse<StandardPostDisplayDto>> GetByUserIdAsync(int userId, int? callerId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardPostDisplayDto>();

        bool userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            var posts    = await _postRepository.GetByUserIdAsync(userId, callerId, pageNumber, pageSize);
            var postDtos = posts.Select(PostMapper.ToStandardDisplay).ToList();
            
            response.Ok(postDtos, ResponseMessages.UserPostsRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetByUserIdAsync), nameof(Post), $"Could not retrieve posts. {response.Message}", null);
        }
        
        return  response;
    }

    public async Task<ApplicationResponse<FullPostDisplayDto>> UpdatePost(int id, int userId, UpdatePostDto updatePostDto)
    {
        var response = new ApplicationResponse<FullPostDisplayDto>();

        var postWithStats = await _postRepository.GetPostByIdAsync(id, userId);
        if (postWithStats is not null)
        {
            var  post            = postWithStats.Post;
            bool belongsToCaller = post.UserId == userId;
            if (belongsToCaller)
            {
                post.PostTitle = updatePostDto.Title;
                post.PostContent = updatePostDto.Content;
                post.LastUpdatedAt = DateTime.UtcNow;

                await _postRepository.SaveChangesAsync();

                var postDisplayDto = PostMapper.ToFullDisplay(postWithStats);
                response.Ok(postDisplayDto, ResponseMessages.PostUpdated);
                await LogResultAsync(response.Succeeded, nameof(UpdatePost), nameof(Post), null, id);
            }
            else
            {
                response.Fail(ResponseMessages.CouldNotUpdatePost);
                await LogResultAsync(response.Succeeded, nameof(UpdatePost), nameof(Post), $"{response.Message}. Caller does not have edit permissions to this post", id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(UpdatePost), nameof(Post), response.Message, id);
        }

        return response;
    }

    public async Task<ApplicationResponse> DeletePostAsync(int callerId, bool isAdmin, int id)
    {
        var response = new ApplicationResponse();
        var post     = await _postRepository.GetByIdAsync(id);

        if (post is not null)
        {
            bool belongsToCaller = post.UserId == callerId;
            if (belongsToCaller || isAdmin)
            {
                await _postRepository.ExecuteInTransactionAsync(async () =>
                {
                    await _commentRepository.DeletePostCommentsAsync(post.Id);
                    await _postRepository.DeleteWithoutChangeTrackingAsync(post.Id);
                });

                response.Ok(ResponseMessages.PostDeleted);
                await LogResultAsync(response.Succeeded, nameof(DeletePostAsync), nameof(Post), null, id);
            }
            else
            {
                response.Fail(ResponseMessages.CouldNotDeletePost);
                await LogResultAsync(response.Succeeded, nameof(DeletePostAsync), nameof(Post), $"{response.Message}: Caller does not have delete permissions to the post", id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(DeletePostAsync), nameof(Post), response.Message, id);
        }

        return response;
    }

    // Every rule a new post must meet, images included. Nothing is stored until all of them pass.
    private async Task<ApplicationResponse<List<CheckedImage>>> CheckNewPostAsync(int userId, CreatePostDto createPostDto)
    {
        var response = new ApplicationResponse<List<CheckedImage>>();

        bool userExists = await _userRepository.ExistsByIdAsync(userId);
        bool hasTitle   = !string.IsNullOrWhiteSpace(createPostDto.PostTitle);
        bool hasText    = !string.IsNullOrWhiteSpace(createPostDto.PostContent);
        bool hasImages  = createPostDto.Images.Count > 0;

        if (userExists)
        {
            if (hasTitle)
            {
                if (hasText || hasImages)
                {
                    response = await _imageStorage.CheckPostImagesAsync(createPostDto.Images);
                }
                else
                {
                    response.Fail(ResponseMessages.TextOrImageRequired);
                }
            }
            else
            {
                response.Fail(ResponseMessages.TitleRequired);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }
}

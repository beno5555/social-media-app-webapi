using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class PostService : BaseService
{
    private readonly PostRepository       _postRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly UserRepository       _userRepository;
    private readonly CommentRepository    _commentRepository;

    public PostService(
        PostRepository postRepository,
        FriendshipRepository friendshipRepository,
        UserRepository userRepository,
        CommentRepository commentRepository,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _postRepository = postRepository;
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
    }
    
    public async Task<ApplicationResponse<StandardPostDisplayDto>> UploadPost(int userId, CreatePostDto createPostDto)
    {
        var response = new ApplicationResponse<StandardPostDisplayDto>();
        
        var userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            if (!string.IsNullOrEmpty(createPostDto.PostTitle))
            {
                if (!string.IsNullOrWhiteSpace(createPostDto.PostContent))
                {
                    var post      = PostMapper.ToEntity(userId, createPostDto);
                    var addedPost = await _postRepository.AddPostAsync(post);

                    var postDisplay = PostMapper.ToStandardDisplay(addedPost);
                    response.Ok(postDisplay, ResponseMessages.PostUploaded);
                }
                else
                {
                    response.Fail(ResponseMessages.ContentRequired);
                    await LogResultAsync(response.Succeeded, nameof(UploadPost), nameof(Post), $"Could not upload post: {response.Message}", null);
                }
            }
            else
            {
                response.Fail(ResponseMessages.TitleRequired);
                await LogResultAsync(response.Succeeded, nameof(UploadPost), nameof(Post), $"Could not upload post: {response.Message}", null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(UploadPost), nameof(Post), $"Could not upload post: {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullPostDisplayDto>> GetPostByIdAsync(int id)
    {
        var response = new ApplicationResponse<FullPostDisplayDto>();

        var post = await _postRepository.GetPostByIdAsync(id);
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

        var posts    = await _postRepository.GetFeedAsync(friendIds, pageNumber, pageSize);
        var postDtos = posts.Select(PostMapper.ToStandardDisplay).ToList();

        response.Ok(postDtos, ResponseMessages.Feed);

        return response;
    }
    
    public async Task<ListResponse<StandardPostDisplayDto>> GetByUserIdAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardPostDisplayDto>();

        bool userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            var posts    = await _postRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
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

        var post = await _postRepository.GetPostByIdAsync(id);
        if (post is not null)
        {
            bool belongsToCaller = post.UserId == userId;
            if (belongsToCaller)
            {
                post.PostTitle = updatePostDto.Title;
                post.PostContent = updatePostDto.Content;
                post.LastUpdatedAt = DateTime.UtcNow;

                await _postRepository.SaveChangesAsync();
                
                var postDisplayDto = PostMapper.ToFullDisplay(post);
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
}
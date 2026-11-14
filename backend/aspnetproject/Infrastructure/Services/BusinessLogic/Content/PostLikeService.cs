using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class PostLikeService : BaseService
{
    private readonly PostLikeRepository      _postLikeRepository;
    private readonly PostRepository          _postRepository;
    private readonly IHubContext<MessageHub> _hubContext;

    public PostLikeService(
        PostLikeRepository postLikeRepository,
        PostRepository postRepository,
        IHubContext<MessageHub> hubContext,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _postLikeRepository = postLikeRepository;
        _postRepository = postRepository;
        _hubContext = hubContext;
    }

    // Every action reads a hidden post the caller may not see as missing.
    public async Task<ApplicationResponse> LikePostAsync(int userId, bool isAdmin, int postId)
    {
        var response = new ApplicationResponse();

        var post = await _postRepository.GetReadablePostAsync(postId, userId, isAdmin);

        if (post is not null)
        {
            bool notLikedYet = !await _postLikeRepository.HasUserLikedPostAsync(postId, userId);

            // A repeated like succeeds without adding a row or sending a push.
            if (notLikedYet)
            {
                var like      = PostLikeMapper.ToEntity(postId, userId);
                var addedLike = await _postLikeRepository.AddLikeAsync(like);

                await NotifyAuthorAsync(post, addedLike);
            }

            response.Ok(ResponseMessages.PostLiked);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(LikePostAsync), nameof(PostLike), $"Could not like post {postId}. {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse> UnlikePostAsync(int userId, bool isAdmin, int postId)
    {
        var response = new ApplicationResponse();

        bool postIsReadable = await _postRepository.IsReadableAsync(postId, userId, isAdmin);

        if (postIsReadable)
        {
            bool wasLiked = await _postLikeRepository.DeleteLikeAsync(postId, userId);

            if (wasLiked)
            {
                response.Ok(ResponseMessages.PostUnliked);
            }
            else
            {
                response.Fail(ResponseMessages.PostNotLiked);
                await LogResultAsync(response.Succeeded, nameof(UnlikePostAsync), nameof(PostLike), $"Could not unlike post {postId}. {response.Message}", null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(UnlikePostAsync), nameof(PostLike), $"Could not unlike post {postId}. {response.Message}", null);
        }

        return response;
    }

    public async Task<ListResponse<MinimalUserDto>> GetLikersAsync(int postId, int? callerId, bool isAdmin, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<MinimalUserDto>();

        bool postIsReadable = await _postRepository.IsReadableAsync(postId, callerId, isAdmin);

        if (postIsReadable)
        {
            var likes  = await _postLikeRepository.GetByPostIdAsync(postId, pageNumber, pageSize);
            var likers = likes.Select(PostLikeMapper.ToLikerDisplay).ToList();

            response.Ok(likers, ResponseMessages.PostLikesRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetLikersAsync), nameof(PostLike), $"Could not retrieve likes of post {postId}. {response.Message}", null);
        }

        return response;
    }

    // The author is told about likes by other users only. The count is read after the like is saved, so it includes it.
    private async Task NotifyAuthorAsync(Post post, PostLike like)
    {
        bool likedBySomeoneElse = like.UserId != post.UserId;

        if (likedBySomeoneElse)
        {
            int likeCount    = await _postLikeRepository.CountByPostIdAsync(post.Id);
            var notification = PostLikeMapper.ToNotification(like, likeCount);

            await _hubContext.Clients.Group(post.UserId.ToString()).SendAsync("PostLiked", notification);
        }
    }
}

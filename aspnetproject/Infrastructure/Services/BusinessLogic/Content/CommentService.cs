using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class CommentService : BaseService
{
    private readonly CommentRepository       _commentRepository;
    private readonly PostRepository          _postRepository;
    private readonly UserRepository          _userRepository;
    private readonly IHubContext<MessageHub> _hubContext;

    public CommentService(
        CommentRepository commentRepository,
        PostRepository postRepository,
        UserRepository userRepository,
        IHubContext<MessageHub> hubContext,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _hubContext = hubContext;
    }
    
    public async Task<ApplicationResponse<FullCommentDto>> GetCommentByIdAsync(int id)
    {
        var response = new ApplicationResponse<FullCommentDto>();
        
        var comment = await _commentRepository.GetCommentByIdAsync(id);

        if (comment is not null)
        {
            var commentDto = CommentMapper.ToFullDisplay(comment);           
            response.Ok(commentDto, ResponseMessages.CommentRetrieved);
            await LogResultAsync(response.Succeeded, nameof(GetCommentByIdAsync), nameof(Comment), null, comment.Id);
        }
        else
        {
            response.Fail(ResponseMessages.CommentNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetCommentByIdAsync), nameof(Comment), $"Could not retrieve comment from the database. {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullCommentDto>> AddCommentAsync(int commentAuthorId, int postId, CreateCommentDto createCommentDto)
    {
        var response = new ApplicationResponse<FullCommentDto>();

        var userExists = await _userRepository.ExistsByIdAsync(commentAuthorId);
        
        if (userExists)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            
            if (post is not null)
            {
                var comment      = CommentMapper.ToEntity(commentAuthorId, postId, createCommentDto);
                var addedComment = await _commentRepository.AddCommentAsync(comment);

                if (commentAuthorId != post.UserId)
                {
                    var commentNotification = CommentMapper.ToNotification(addedComment);
                    // await _hubContext.Clients.Group(post.UserId.ToString()).SendAsync("ReceiveComment", commentNotification); // temporarily off to not interfere with integration tests
                }
            
                var commentDto = CommentMapper.ToFullDisplay(addedComment);
                response.Ok(commentDto, ResponseMessages.CommentUploaded);
            }
            else
            {
                response.Fail(ResponseMessages.PostNotFound);
                await LogResultAsync(response.Succeeded, nameof(AddCommentAsync), nameof(Comment), $"Could not add comment to the post. {response.Message}", null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(AddCommentAsync), nameof(Comment), $"Could not add comment to the post. {response.Message}", null);
        }
        
        return response;
    }

    public async Task<ApplicationResponse> DeleteCommentAsync(int userId, bool isAdmin, int id)
    {
        var response = new ApplicationResponse();

        var comment = await _commentRepository.GetCommentWithCommenterUserAndPostById(id);

        if (comment is not null)
        {
            bool belongsToCaller     = comment.CommenterUserId == userId;
            bool isPostAuthor        = comment.Post!.UserId    == userId;
            bool hasDeletePermission = belongsToCaller || isPostAuthor || isAdmin;
            
            if (hasDeletePermission)
            {
                await _commentRepository.DeleteAsync(comment);  
                response.Ok(ResponseMessages.CommentDeletedSuccessfully);
                await LogResultAsync(response.Succeeded, nameof(DeleteCommentAsync), nameof(Comment), null, comment.Id);
            }
            else
            {
                response.Fail(ResponseMessages.CouldNotDeleteComment);
                await LogResultAsync(response.Succeeded, nameof(DeleteCommentAsync), nameof(Comment), $"{response.Message}: caller does not have the privileges to delete a comment", comment.Id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.CommentNotFound);
            await LogResultAsync(response.Succeeded, nameof(DeleteCommentAsync), nameof(Comment), $"{response.Message}.", null);
        }

        return response;
    }

    public async Task<ListResponse<StandardCommentDto>> GetByPostAsync(int postId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardCommentDto>();

        var postExists = await _postRepository.ExistsByIdAsync(postId);

        if (postExists)
        {
            var comments    = await _commentRepository.GetByPostIdAsync(postId, pageNumber, pageSize);
            var commentDtos = comments.Select(CommentMapper.ToStandardDisplay).ToList();
            
            response.Ok(commentDtos, ResponseMessages.PostCommentsRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetByPostAsync), nameof(Comment), $"Could not retrieve comments. {response.Message}", null);
        }

        return response;
    }

    public async Task<ListResponse<StandardCommentDto>> GetByUserAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardCommentDto>();

        var userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            var comments    = await _commentRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
            var commentDtos = comments.Select(CommentMapper.ToStandardDisplay).ToList();

            response.Ok(commentDtos, ResponseMessages.UserCommentsRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetByPostAsync), nameof(Comment), $"Could not retrieve comments. {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullCommentDto>> EditCommentAsync(int userId, int id, EditCommentDto editCommentDto)
    {
        var response = new ApplicationResponse<FullCommentDto>();
        
        var comment = await _commentRepository.GetCommentByIdAsync(id);

        if (comment is not null)
        {
            bool belongsToCaller = comment.CommenterUserId == userId;
            if (belongsToCaller)
            {
                comment.CommentContent = editCommentDto.Content;
                comment.LastUpdatedAt = DateTime.UtcNow;
            
                await _commentRepository.SaveChangesAsync();
            
                var commentDto = CommentMapper.ToFullDisplay(comment);
                response.Ok(commentDto, ResponseMessages.CommentUpdated);
                await LogResultAsync(response.Succeeded, nameof(EditCommentAsync), nameof(Comment), null, comment.Id);
            }
            else
            {
                response.Fail(ResponseMessages.OnlyCommenterCanEditMessage);
                await LogResultAsync(response.Succeeded, nameof(EditCommentAsync), nameof(Comment), response.Message, comment.Id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.CommentNotFound);
            await LogResultAsync(response.Succeeded, nameof(EditCommentAsync), nameof(Comment), $"{response.Message}", null);
        }

        return response;
    }
}
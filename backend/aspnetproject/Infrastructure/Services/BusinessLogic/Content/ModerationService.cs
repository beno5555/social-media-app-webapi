using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

// An administrator's direct status change, and the queue of flagged content to decide on.
// Every read applies the resulting visibility rule; this class only stores it.
public class ModerationService : BaseService
{
    private readonly PostRepository    _postRepository;
    private readonly CommentRepository _commentRepository;

    public ModerationService(
        PostRepository postRepository,
        CommentRepository commentRepository,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _postRepository    = postRepository;
        _commentRepository = commentRepository;
    }

    public async Task<ApplicationResponse> SetPostStatusAsync(int id, ModerationStatus status)
    {
        var response = new ApplicationResponse();
        var post     = await _postRepository.GetByIdAsync(id);

        if (post is not null)
        {
            var previousStatus = post.ModerationStatus;

            post.ModerationStatus = status;
            await _postRepository.SaveChangesAsync();

            response.Ok(ResponseMessages.ModerationStatusUpdated);
            await LogResultAsync(response.Succeeded, nameof(SetPostStatusAsync), nameof(Post), StatusChangeDetails(previousStatus, status), id);
        }
        else
        {
            response.Fail(ResponseMessages.PostNotFound);
            await LogResultAsync(response.Succeeded, nameof(SetPostStatusAsync), nameof(Post), response.Message, id);
        }

        return response;
    }

    public async Task<ApplicationResponse> SetCommentStatusAsync(int id, ModerationStatus status)
    {
        var response = new ApplicationResponse();
        var comment  = await _commentRepository.GetByIdAsync(id);

        if (comment is not null)
        {
            var previousStatus = comment.ModerationStatus;

            comment.ModerationStatus = status;
            await _commentRepository.SaveChangesAsync();

            response.Ok(ResponseMessages.ModerationStatusUpdated);
            await LogResultAsync(response.Succeeded, nameof(SetCommentStatusAsync), nameof(Comment), StatusChangeDetails(previousStatus, status), id);
        }
        else
        {
            response.Fail(ResponseMessages.CommentNotFound);
            await LogResultAsync(response.Succeeded, nameof(SetCommentStatusAsync), nameof(Comment), response.Message, id);
        }

        return response;
    }

    // The two queue reads write no Log row: a successful read never does.
    public async Task<ListResponse<PostSnapshotDto>> GetFlaggedPostsAsync(PageQuery pageQuery)
    {
        var response = new ListResponse<PostSnapshotDto>();

        var posts     = await _postRepository.GetFlaggedAsync(pageQuery.PageNumber, pageQuery.PageSize);
        var snapshots = posts.Select(SnapshotMapper.ToPostSnapshot).ToList();

        response.Ok(snapshots, ResponseMessages.ModerationQueueRetrieved);

        return response;
    }

    public async Task<ListResponse<CommentSnapshotDto>> GetFlaggedCommentsAsync(PageQuery pageQuery)
    {
        var response = new ListResponse<CommentSnapshotDto>();

        var comments  = await _commentRepository.GetFlaggedAsync(pageQuery.PageNumber, pageQuery.PageSize);
        var snapshots = comments.Select(SnapshotMapper.ToCommentSnapshot).ToList();

        response.Ok(snapshots, ResponseMessages.ModerationQueueRetrieved);

        return response;
    }

    private string StatusChangeDetails(ModerationStatus previousStatus, ModerationStatus status)
    {
        return $"Moderation status changed from {previousStatus} to {status}";
    }
}

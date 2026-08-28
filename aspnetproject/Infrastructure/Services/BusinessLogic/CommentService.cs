using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Comments;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class CommentService
{
    private readonly CommentRepository _commentRepository;
    private readonly PostRepository    _postRepository;
    private readonly UserRepository    _userRepository;
    private readonly CommentMapper     _commentMapper;

    public CommentService(
        CommentRepository commentRepository,
        PostRepository postRepository,
        UserRepository userRepository,
        
        CommentMapper commentMapper
        )
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        
        _commentMapper = commentMapper;
    }
    
    public async Task<ApplicationResponse<FullCommentDto>> GetCommentByIdAsync(int id)
    {
        var response = new ApplicationResponse<FullCommentDto>();
        
        var comment = await _commentRepository.GetCommentByIdAsync(id);

        if (comment is not null)
        {
            var commentDto = _commentMapper.ToFullDisplay(comment);           
            response.Ok(commentDto, "Comment retrieved successfully!");
        }

        return response;
    }

    // no author check. commenting on behalf of someone else is not a feature and authenticated user does not require id check
    public async Task<ApplicationResponse<FullCommentDto>> AddCommentAsync(int authorId, CreateCommentDto createCommentDto)
    {
        var response   = new ApplicationResponse<FullCommentDto>();
        var postExists = await _postRepository.ExistsByIdAsync(createCommentDto.PostId);

        if (postExists)
        {
            var comment = _commentMapper.ToEntity(authorId, createCommentDto);
            var addedComment = await _commentRepository.AddCommentAsync(comment);
            
            var commentDto = _commentMapper.ToFullDisplay(addedComment);
            response.Ok(commentDto, "Comment uploaded successfully!");
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }

    public async Task<ApplicationResponse> DeleteCommentAsync(int userId, int id)
    {
        var response = new ApplicationResponse();

        var comment = await _commentRepository.GetByIdAsync(id);

        if (comment is not null)
        {
            bool belongsToCaller = comment.CommenterUserId == userId;
            if (belongsToCaller)
            {
                await _commentRepository.DeleteAsync(comment);  
                response.Ok("Comment deleted successfully!");
            }
            else
            {
                response.Fail("Comment could not be deleted");
            }
        }
        else
        {
            response.Fail("Comment not found");
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
            var commentDtos = comments.Select(_commentMapper.ToStandardDisplay).ToList();
            
            response.Ok(commentDtos);
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }

    public async Task<ListResponse<StandardCommentDto>> GetByUserAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<StandardCommentDto>();

        var userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            var comments = await _commentRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
            var commentDtos = comments.Select(_commentMapper.ToStandardDisplay).ToList();

            response.Ok(commentDtos, "User comments retrieved successfully!");
        }
        else
        {
            response.Fail("User not found");
        }

        return response;
    }

    public async Task<ApplicationResponse<FullCommentDto>> EditCommentAsync(int id, EditCommentDto editCommentDto)
    {
        var response = new ApplicationResponse<FullCommentDto>();
        
        var comment = await _commentRepository.GetCommentByIdAsync(id);

        if (comment is not null)
        {
            comment.CommentContent = editCommentDto.Content;
            comment.LastUpdatedAt = DateTime.UtcNow;
            
            await _commentRepository.SaveChangesAsync();
            
            var commentDto = _commentMapper.ToFullDisplay(comment);
            response.Ok(commentDto, "Commend updated successfully!");
        }

        return response;
    }
}
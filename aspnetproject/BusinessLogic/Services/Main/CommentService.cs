using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services;

public class CommentService
{
    private readonly CommentRepository _commentRepository;
    private readonly PostRepository    _postRepository;
    private readonly CommentMapper     _commentMapper;

    public CommentService(CommentRepository commentRepository, PostRepository postRepository, CommentMapper commentMapper)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _commentMapper = commentMapper;
    }

    public async Task<ApplicationResponse> AddCommentAsync(CreateCommentDto createCommentDto)
    {
        var response   = new ApplicationResponse();
        var postExists = await _postRepository.ExistsByIdAsync(createCommentDto.PostId);

        if (postExists)
        {
            var comment = _commentMapper.ToEntity(createCommentDto);
            await _commentRepository.AddAsync(comment);
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }

    public async Task<ApplicationResponse> DeleteCommentAsync(int commentId)
    {
        var response = new ApplicationResponse();

        var comment = await _commentRepository.GetByIdAsync(commentId);

        if (comment is not null)
        {
            await _commentRepository.DeleteAsync(comment);  
        }
        else
        {
            response.Fail("Comment not found");
        }

        return response;
    }

    public async Task<ApplicationResponse<List<DisplayCommentDto>>> GetByPostAsync(int postId, int? pageNumber, int? pageSize)
    {
        var response = new ApplicationResponse<List<DisplayCommentDto>>();

        var postExists = await _postRepository.ExistsByIdAsync(postId);

        if (postExists)
        {
            var comments    = await _commentRepository.GetByPostIdAsync(postId, pageNumber, pageSize);
            var commentDtos = comments.Select(_commentMapper.ToDisplay).ToList();
            response.Ok(commentDtos);
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }
}
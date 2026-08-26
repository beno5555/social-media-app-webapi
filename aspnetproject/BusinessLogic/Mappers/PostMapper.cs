using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.BusinessLogic.Dtos.Posts;
using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Common.Dtos.Posts;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class PostMapper
{
    public Post ToEntity(int userId, CreatePostDto createPostDto)
    {
        return new Post
        {
            UserId = userId,
            PostTitle = createPostDto.PostTitle,
            PostContent = createPostDto.PostContent,
        };
    }

    public FullPostDisplayDto ToFullDisplay(Post post)
    {
        return new FullPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            UserId = post.UserId,
            Username = post.User!.Username,

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
            
            Comments = post.Comments.Select(comment => new DisplayCommentDto
            {
                Id = comment.Id,
                AuthorUsername = comment.CommenterUser!.Username,
                Content = comment.CommentContent,
                UploadedAt = comment.CreatedAt
            }).ToList()
        };
    }
    
    public FullPostDisplayDto ToFullDisplay(Post post, List<DisplayCommentDto> commentDtos)
    {
        return new FullPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            UserId = post.UserId,
            Username = post.User!.Username,

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
            
            Comments = commentDtos
        };
    }
    
    public StandardPostDisplayDto ToStandardDisplay(Post post)
    {
        return new StandardPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            UserId = post.UserId,
            Username = post.User!.Username,

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
        };
    }
    
    public MinimalPostDisplayDto ToMinimalDisplay(Post post)
    {
        return new MinimalPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,
            UserId = post.UserId,
            UploadedAt = post.CreatedAt,
        };
    }

}
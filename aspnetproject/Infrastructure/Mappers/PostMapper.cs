using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

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

            Author = new MinimalUserDto
            {
                Id = post.UserId,
                Username = post.User!.Username,
            },

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
            
            Comments = post.Comments.Select(comment => new StandardCommentDto
            {
                Id = comment.Id,
                AuthorUsername = comment.CommenterUser!.Username,
                Content = comment.CommentContent,
                UploadedAt = comment.CreatedAt
            }).ToList()
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
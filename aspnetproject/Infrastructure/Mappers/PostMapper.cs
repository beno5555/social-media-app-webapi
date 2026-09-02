using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

public static class PostMapper
{
    public static Post ToEntity(int userId, CreatePostDto createPostDto)
    {
        return new Post
        {
            UserId = userId,
            PostTitle = createPostDto.PostTitle,
            PostContent = createPostDto.PostContent,
        };
    }

    public static FullPostDisplayDto ToFullDisplay(Post post)
    {
        return new FullPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            Author = new MinimalUserDto
            {
                Id = post.UserId,
                Username = UsernameMapper.ResolveUsername(post.User, post.UserId)
            },

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
            
            Comments = post.Comments.Select(comment => new StandardCommentDto
            {
                Id =  comment.Id,
                Author = new MinimalUserDto
                {
                    Id = comment.CommenterUserId,
                    Username =  UsernameMapper.ResolveUsername(comment.CommenterUser, comment.CommenterUserId)
                },
                Content = comment.CommentContent,
                UploadedAt = comment.CreatedAt
            }).ToList()
        };
    }
    
    public static StandardPostDisplayDto ToStandardDisplay(Post post)
    {
        return new StandardPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            Author = new MinimalUserDto
            {
                Id = post.UserId,
                Username = UsernameMapper.ResolveUsername(post.User, post.UserId)
            },

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
        };
    }
    
    public static MinimalPostDisplayDto ToMinimalDisplay(Post post)
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
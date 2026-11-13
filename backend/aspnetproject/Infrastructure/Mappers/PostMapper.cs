using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Dtos.Images;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Dtos.Users;

namespace aspnetproject.Infrastructure.Mappers;

public static class PostMapper
{
    public static Post ToEntity(int userId, CreatePostDto createPostDto, IReadOnlyList<string> imageAddresses)
    {
        return new Post
        {
            UserId = userId,
            PostTitle = createPostDto.PostTitle,
            PostContent = createPostDto.PostContent ?? string.Empty,
            Images = imageAddresses
                .Select((address, index) => new PostImage { Address = address, OrderIndex = index })
                .ToList()
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
            Images = ToImageDtos(post),
            
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
            Images = ToImageDtos(post),
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

    private static List<ImageDto> ToImageDtos(Post post)
    {
        return post.Images
            .OrderBy(image => image.OrderIndex)
            .Select(image => new ImageDto
            {
                Id = image.Id,
                Address = image.Address,
                OrderIndex = image.OrderIndex
            })
            .ToList();
    }
}

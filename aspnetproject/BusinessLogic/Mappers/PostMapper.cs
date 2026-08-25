using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.BusinessLogic.Dtos.Posts;
using aspnetproject.BusinessLogic.Mappers.Base;
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

    public DetailedPostDisplayDto ToDisplay(Post post)
    {
        return new DetailedPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,

            UserId = post.UserId,
            Username = post.User!.Username,

            PostContent = post.PostContent,
            UploadedAt = post.CreatedAt,
        };
    }
    
    public DetailedPostDisplayDto ToDisplay(Post post, List<DisplayCommentDto> commentDtos)
    {
        return new DetailedPostDisplayDto
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

    public SummarizedPostDisplayDto ToSummarizedDisplay(Post post)
    {
        return new SummarizedPostDisplayDto
        {
            Id = post.Id,
            Title = post.PostTitle,
            UserId = post.UserId,
            UploadedAt = post.CreatedAt,
        };
    }

}
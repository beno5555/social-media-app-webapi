using aspnetproject.BusinessLogic.Dtos.CommentDtos;
using aspnetproject.BusinessLogic.Dtos.PostDtos;
using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class PostMapper : IMapper<Post, CreatePostDto, DisplayPostDto>
{
    public Post ToEntity(CreatePostDto createPostDto)
    {
        return new Post
        {
            UserId = createPostDto.UserId,
            PostTitle = createPostDto.PostTitle,
            PostContent = createPostDto.PostContent,
        };
    }

    public DisplayPostDto ToDisplay(Post post)
    {
        return new DisplayPostDto(post.Id, post.User!.Username, post.PostTitle, post.PostContent, post.CreatedAt);
    }

    public DisplayPostDto ToDisplay(Post post, List<DisplayCommentDto> commentDtos)
    {
        return new DisplayPostDto(post.Id, post.User!.Username, post.PostTitle, post.PostContent, post.CreatedAt, commentDtos);
    }
}
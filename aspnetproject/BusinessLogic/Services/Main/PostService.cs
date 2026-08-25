using aspnetproject.BusinessLogic.Dtos.Posts;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services.Main;

public class PostService
{
    private readonly PostRepository       _postRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly UserRepository       _userRepository;
    private readonly CommentRepository    _commentRepository;
    private readonly PostMapper           _postMapper;

    public PostService(PostRepository postRepository, FriendshipRepository friendshipRepository, UserRepository userRepository, CommentRepository commentRepository, PostMapper postMapper)
    {
        _postRepository = postRepository;
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _postMapper = postMapper;
    }

    public async Task<ApplicationResponse<DetailedPostDisplayDto>> GetPostByIdAsync(int id)
    {
        var response = new ApplicationResponse<DetailedPostDisplayDto>();

        var post = await _postRepository.GetPostByIdAsync(id);
        if (post is not null)
        {
            var postDisplay = _postMapper.ToDisplay(post);
            response.Ok(postDisplay, "Post retrieved successfully");
        }
        else
        {
            response.Fail("Post not found");
        }
        return response;
    }

    /// <summary>
    /// We do not check whether user with userId exists in the database or not, since the only source of userId is the userId of the currently logged-in user.
    /// Proper id checking will be implemented if we decide to add admin role that would be able to upload/update the post under someone else's name
    /// </summary>
    public async Task<ApplicationResponse<DetailedPostDisplayDto>> UploadPost(int userId, CreatePostDto createPostDto)
    {
        var response = new ApplicationResponse<DetailedPostDisplayDto>();
        
        if (!string.IsNullOrWhiteSpace(createPostDto.PostTitle))
        {
            if (!string.IsNullOrWhiteSpace(createPostDto.PostContent))
            {
                var post = _postMapper.ToEntity(userId, createPostDto);
                await _postRepository.AddAsync(post);

                var postDisplay = _postMapper.ToDisplay(post);
                response.Ok(postDisplay, "Post uploaded successfully");
            }
            else
            {
                response.Fail("Post content is required");
            }
        }
        else
        {
            response.Fail("Post title is required");
        }

        return response;
    }

    public async Task<ApplicationResponse<DetailedPostDisplayDto>> UpdatePost(int id, int userId, UpdatePostDto updatePostDto)
    {
        var response = new ApplicationResponse<DetailedPostDisplayDto>();

        var post = await _postRepository.GetByIdAsync(id);
        if (post is not null)
        {
            bool belongsToCaller = post.UserId == userId;
            if (belongsToCaller)
            {
                post.PostTitle = updatePostDto.Title;
                post.PostContent = updatePostDto.Content;
                
                var postDisplayDto = _postMapper.ToDisplay(post);
                response.Ok(postDisplayDto, "Post updated successfully");
            }
            else
            {
                response.Fail("Post cannot be updated");
            }
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }

    public async Task<ListResponse<SummarizedPostDisplayDto>> GetAllPostsAsync(int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<SummarizedPostDisplayDto>();
        
        var posts    = await _postRepository.GetPaginatedAsync(pageNumber, pageSize);
        var postDtos = posts
            .Select(_postMapper
                .ToSummarizedDisplay)
            .ToList();

        response.Data = postDtos;

        return response;
    }

    public async Task<ListResponse<DetailedPostDisplayDto>> GetFeedAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<DetailedPostDisplayDto>();
        
        var friends  = await _friendshipRepository.GetFriendshipsAsync(userId);
        List<int> friendIds = friends.Select(friend =>
            friend.RequesterUserId == userId ? friend.AddresseeUserId : friend.RequesterUserId).ToList();

        var posts = await _postRepository.GetFeedAsync(friendIds, pageNumber, pageSize);
        var postDtos = posts.Select(_postMapper.ToDisplay).ToList();

        response.Data = postDtos;

        return response;
    }

    public async Task<ApplicationResponse<List<DetailedPostDisplayDto>>> GetByUserIdAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ApplicationResponse<List<DetailedPostDisplayDto>>();

        bool userExists = await _userRepository.ExistsByIdAsync(userId);

        if (userExists)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
            
            if (posts.Count > 0)
            {
                var postDtos = posts.Select(_postMapper.ToDisplay).ToList();
                response.Ok(postDtos);
            }
            else
            {
                response.Fail("No posts.");
            }
        }
        else
        {
            response.Fail("Invalid user id");
        }
        
        return  response;
    }

    public async Task<ApplicationResponse> DeletePostAsync(int userId, int id)
    {
        var response = new ApplicationResponse();
        var post     = await _postRepository.GetByIdAsync(id);

        if (post is not null)
        {
            bool belongsToCaller = post.UserId == userId;
            if (belongsToCaller)
            {
                await _postRepository.ExecuteInTransactionAsync(async () =>
                {
                    await _commentRepository.DeletePostCommentsAsync(post.Id);
                    await _postRepository.DeleteWithoutChangeTrackingAsync(post.Id);
                });
            }
            else
            {
                response.Fail("Post could not be deleted");
            }
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }
}
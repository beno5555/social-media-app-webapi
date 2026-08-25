using aspnetproject.BusinessLogic.Dtos.PostDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services;

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

    /// <summary>
    /// We do not check whether user with userId exists in the database or not, since the only source of userId is the userId of the currently logged-in user.
    /// Proper id checking will be implemented if we decide to add admin role that would be able to upload/update the post under someone else's name
    /// </summary>
    /// <param name="createPostDto"></param>
    /// <returns></returns>
    public async Task<ApplicationResponse> UploadPost(CreatePostDto createPostDto)
    {
        var response = new ApplicationResponse();
        
        if (!string.IsNullOrWhiteSpace(createPostDto.PostTitle))
        {
            if (!string.IsNullOrWhiteSpace(createPostDto.PostContent))
            {
                var post = _postMapper.ToEntity(createPostDto);
                await _postRepository.AddAsync(post);
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

    public async Task<List<DisplayPostDto>> GetFeedAsync(int userId, int? pageNumber, int? pageSize)
    {
        var friends = await _friendshipRepository.GetFriendshipsAsync(userId);
        List<int> friendIds = friends.Select(friend =>
            friend.RequesterUserId == userId ? friend.AddresseeUserId : friend.RequesterUserId).ToList();

        var posts = await _postRepository.GetFeedAsync(friendIds, pageNumber, pageSize);
        var postDtos = posts.Select(_postMapper.ToDisplay).ToList();

        return postDtos;
    }

    public async Task<ApplicationResponse<List<DisplayPostDto>>> GetByUserIdAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ApplicationResponse<List<DisplayPostDto>>();

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

    public async Task<ApplicationResponse> DeletePostAsync(int postId)
    {
        var response = new ApplicationResponse();
        var post     = await _postRepository.GetByIdAsync(postId);

        if (post is not null)
        {
            await _postRepository.ExecuteInTransactionAsync(async () =>
            {
                await _commentRepository.DeletePostCommentsAsync(post.Id);
                await _postRepository.DeleteWithoutChangeTrackingAsync(post.Id);
            });
        }
        else
        {
            response.Fail("Post not found");
        }

        return response;
    }
}
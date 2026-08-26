using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.Common.Dtos.Friendships;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Models;
using aspnetproject.ProjectConstants.Enums;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services.Main;

public class FriendshipService
{
    private readonly FriendshipRepository _friendshipRepository;
    private readonly MessageRepository    _messageRepository;
    private readonly UserRepository       _userRepository;
    
    private readonly UserMapper       _userMapper;
    private readonly FriendshipMapper _friendshipMapper;

    public FriendshipService(
        FriendshipRepository friendshipRepository,
        MessageRepository messageRepository,
        UserMapper userMapper,
        UserRepository userRepository,
        FriendshipMapper friendshipMapper
        )
    {
        _friendshipRepository = friendshipRepository;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        
        _userMapper = userMapper;
        _friendshipMapper = friendshipMapper;
    }

    public async Task<ApplicationResponse<MinimalFriendshipDto>> SendRequest(int requesterId, int addresseeId)
    {
        var response = new ApplicationResponse<MinimalFriendshipDto>();

        if (requesterId != addresseeId)
        {
            var addresseeExists = await _userRepository.ExistsByIdAsync(addresseeId);

            if (addresseeExists)
            {
                var relationship = await _friendshipRepository.GetRelationshipAsync(requesterId, addresseeId);

                if (relationship is null)
                {
                    var friendship = _userMapper.ToFriendship(requesterId, addresseeId);
                    await _friendshipRepository.AddAsync(friendship);

                    var friendshipDto = _friendshipMapper.ToMinimalDisplay(friendship);
                    response.Ok(friendshipDto, "Friend request send successfully");
                }
                else
                {
                    response = await HandleExistingRelationship(relationship);
                }
            }
            else
            {
                response.Fail("addressee not found");
            }
        }
        else
        {
            response.Fail("Friend request cannot be sent to oneself");
        }

        return response;
    }

    private async Task<ApplicationResponse<MinimalFriendshipDto>> HandleExistingRelationship(Friendship relationship)
    {
        var response = new ApplicationResponse<MinimalFriendshipDto>();
        
        if (relationship.FriendshipStatus == FriendshipStatus.Accepted)
        {
            response.Fail("You are already friends with this user");
        }
        else if (relationship.FriendshipStatus == FriendshipStatus.Pending)
        {
            response.Fail("A pending friend request already exists");
        }
        else if (relationship.FriendshipStatus == FriendshipStatus.Declined)
        {
            await _friendshipRepository.UpdateStatusAsync(relationship, FriendshipStatus.Pending);
            
            relationship.SentAt = DateTime.UtcNow;
            relationship.LastUpdatedAt = DateTime.UtcNow;
            await _friendshipRepository.SaveChangesAsync();

            var friendshipDto = _friendshipMapper.ToMinimalDisplay(relationship);
            response.Ok(friendshipDto, "Friend request send successfully");
        }

        return response;
    }

    public async Task<ApplicationResponse<StandardFriendshipDto>> RespondToRequestAsync(int requesterId, int addresseeId, FriendshipStatus status)
    {
        var response = new ApplicationResponse<StandardFriendshipDto>();

        if (ValidRequestResponse(status))
        {
            if (requesterId != addresseeId)
            {
                var friendship = await _friendshipRepository.GetRelationshipAsync(requesterId, addresseeId, true);

                if (friendship is not null && friendship.FriendshipStatus == FriendshipStatus.Pending)
                {
                    await _friendshipRepository.UpdateStatusAsync(friendship, status);
                    
                    var friendshipDto = _friendshipMapper.ToStandardDisplay(friendship, friendship.RequesterUser!);
                    response.Ok(friendshipDto, "Responded Sent!");
                }
                else
                {
                    response.Fail("No pending request found");
                }
            }
            else
            {
                response.Fail("Cannot respond to a self-request");
            }
        }

        return response;
    }

    public async Task<ApplicationResponse> RemoveRelationshipAsync(int userId, int friendId)
    {
        var response = new ApplicationResponse();

        var friendship = await _friendshipRepository.GetRelationshipAsync(userId, friendId, false);

        if (friendship is not null)
        {
            await _messageRepository.DeleteConversationAsync(userId, friendId);
            await _friendshipRepository.DeleteAsync(friendship);
        }
        else
        {
            response.Fail("Friendships not found");
        }

        return response;
    }

    public async Task<ListResponse<StandardFriendshipDto>> GetFriendshipsAsync(int userId, int? pageNumber = null, int? pageSize = null)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetFriendshipsAsync, pageNumber, pageSize, "Friendships");
    }

    public async Task<ListResponse<StandardFriendshipDto>> GetPendingRequestsAsync(int userId, int? pageNumber, int? pageSize)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetPendingRequestsAsync, pageNumber, pageSize, "Pending Requests");
    }
    
    public async Task<ListResponse<StandardFriendshipDto>> GetSentRequestsAsync(int userId, int? pageNumber, int? pageSize)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetSentRequestsAsync, pageNumber, pageSize, "Sent Requests");
    }

    private async Task<ListResponse<StandardFriendshipDto>> FetchRelationshipsAsync(
        int userId,
        Func<int, int?, int?, Task<List<Friendship>>> getAsync,
        int? pageNumber,
        int? pageSize,
        string friendshipType = ""
        )
    {
        var response   = new ListResponse<StandardFriendshipDto>();
        
        var userExists = await _userRepository.ExistsByIdAsync(userId);
        
        if (userExists)
        {
            var friendships = await getAsync(userId, pageNumber, pageSize);

            var friendshipDtos = friendships
                .Select(friendship =>
                {
                    var otherUser = friendship.RequesterUserId == userId
                        ? friendship.AddresseeUser
                        : friendship.RequesterUser;

                    var friendshipDto = _friendshipMapper.ToStandardDisplay(friendship, otherUser!);
                    return friendshipDto;
                })
                .ToList();
            
            string friendshipTypeMessage = string.IsNullOrEmpty(friendshipType) ? "Friendships"  : friendshipType;
            response.Ok(friendshipDtos, $"{friendshipTypeMessage} retrieved successfully");
        }
        else
        {
            response.Fail("User not found");
        }
        return response;
    }

    private bool ValidRequestResponse(FriendshipStatus status) =>
        status is FriendshipStatus.Accepted or FriendshipStatus.Declined;
    
    public async Task<ApplicationResponse<StandardFriendshipDto>> GetRelationshipAsync(int currentUserId, int otherUserId, bool orderMatters = false)
    {
        var response = new ApplicationResponse<StandardFriendshipDto>();
        
        var relationship = await _friendshipRepository.GetRelationshipAsync(currentUserId, otherUserId, orderMatters);
        if (relationship is not null)
        {
            var otherUser = relationship.RequesterUserId == otherUserId
                ? relationship.RequesterUser
                : relationship.AddresseeUser;
            
            var relationshipDto = _friendshipMapper.ToStandardDisplay(relationship, otherUser!);
            response.Ok(relationshipDto, "Relationship Fetched Successfully!");
        }
        else
        {
            response.Fail("Relationship not found");
        }

        return response;
    }

    public async Task<bool> AreFriendsAsync(int userA, int userB)
    {
        return await _friendshipRepository.ExistsAsync(userA, userB, FriendshipStatus.Accepted);
    }
}
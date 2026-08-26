using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Models;
using aspnetproject.ProjectConstants.Enums;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services;

public class FriendshipService
{
    private readonly FriendshipRepository _friendshipRepository;
    private readonly MessageRepository    _messageRepository;
    private readonly UserRepository       _userRepository;
    private readonly UserMapper           _userMapper;

    public FriendshipService(FriendshipRepository friendshipRepository, MessageRepository messageRepository, UserMapper userMapper, UserRepository userRepository)
    {
        _friendshipRepository = friendshipRepository;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _userMapper = userMapper;
    }

    public async Task<ApplicationResponse> SendRequest(int requesterId, int addresseeId)
    {
        var response = new ApplicationResponse();

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
                }
                else
                {
                    response = await HandleExistingRelationship(relationship, requesterId);
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

    private async Task<ApplicationResponse> HandleExistingRelationship(Friendship relationship, int requesterId)
    {
        var response = new ApplicationResponse();
        
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
        }

        return response;
    }

    public async Task<ApplicationResponse> RespondToRequestAsync(int requesterId, int addresseeId, FriendshipStatus status)
    {
        var response = new ApplicationResponse();

        if (ValidRequestResponse(status))
        {
            if (requesterId != addresseeId)
            {
                var friendship = await _friendshipRepository.GetRelationshipAsync(requesterId, addresseeId, true);

                if (friendship is not null && friendship.FriendshipStatus == FriendshipStatus.Pending)
                {
                    await _friendshipRepository.UpdateStatusAsync(friendship, status);
                }
                else
                {
                    response.Fail("No pending requests found");
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

        var friendship = await _friendshipRepository.GetRelationshipAsync(userId, friendId);

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

    public async Task<List<DisplayUserDto>> GetFriendsAsync(int userId, int? pageNumber = null, int? pageSize = null)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetFriendshipsAsync, pageNumber, pageSize);
    }

    public async Task<List<DisplayUserDto>> GetPendingRequestUsersAsync(int userId, int? pageNumber,
        int?                                                                      pageSize)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetPendingRequestsAsync, pageNumber, pageSize);
    }
    
    public async Task<List<DisplayUserDto>> GetSentRequestUsersAsync(int userId, int? pageNumber, int? pageSize)
    {
        return await FetchRelationshipsAsync(userId, _friendshipRepository.GetSentRequestsAsync, pageNumber, pageSize);
    }

    private async Task<List<DisplayUserDto>> FetchRelationshipsAsync(int userId, Func<int, int?, int?, Task<List<Friendship>>> getAsync, int? pageNumber, int? pageSize)
    {
        var relationships = await getAsync(userId, pageNumber, pageSize);

        var friends = relationships
            .Select(relationship =>
                relationship.RequesterUserId == userId ? relationship.AddresseeUser : relationship.RequesterUser)
            .OfType<User>()
            .ToList();
    
        var userDtos =  friends.Select(_userMapper.ToDisplay).ToList();

        return userDtos;
    }

    private bool ValidRequestResponse(FriendshipStatus status) =>
        status is FriendshipStatus.Accepted or FriendshipStatus.Declined;
    
    // wrapper
    public async Task<Friendship?> GetRelationshipAsync(int userA, int userB, bool orderMatters = false)
    {
        return await _friendshipRepository.GetRelationshipAsync(userA, userB, orderMatters);
    }

    public async Task<bool> AreFriendsAsync(int userA, int userB)
    {
        return await _friendshipRepository.ExistsAsync(userA, userB, FriendshipStatus.Accepted);
    }
}
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class FriendshipService : BaseService
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
        FriendshipMapper friendshipMapper,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
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
                    response.Ok(friendshipDto, ResponseMessages.FriendRequestSent);
                    await LogResultAsync(response.Succeeded, nameof(SendRequest), nameof(Friendship), $"{response.Message}.", null);
                }
                else
                {
                    response = await HandleExistingRelationship(relationship, requesterId);
                    await LogResultAsync(response.Succeeded, nameof(SendRequest), nameof(Friendship), response.Message, null);
                }
            }
            else
            {
                response.Fail(ResponseMessages.AddresseeNotFound);
                await LogResultAsync(response.Succeeded, nameof(SendRequest), nameof(Friendship), $"Failed to send a request: {response.Message}", null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.FriendRequestCannotBeSentToOneself);
            await LogResultAsync(response.Succeeded, nameof(SendRequest), nameof(Friendship), $"Caller tried to send a request to oneself", null);
        }

        return response;
    }
    private async Task<ApplicationResponse<MinimalFriendshipDto>> HandleExistingRelationship(Friendship relationship, int requesterId)
    {
        var response = new ApplicationResponse<MinimalFriendshipDto>();
        
        if (relationship.FriendshipStatus == FriendshipStatus.Accepted)
        {
            response.Fail(ResponseMessages.AlreadyFriends);
        }
        else if (relationship.FriendshipStatus == FriendshipStatus.Pending)
        {
            response.Fail(ResponseMessages.PendingRequestAlreadyExists);
        }
        else if (relationship.FriendshipStatus == FriendshipStatus.Declined)
        {
            Friendship finalRelationship;
            
            if (relationship.RequesterUserId != requesterId)
            {
                await _friendshipRepository.DeleteAsync(relationship);
                
                int oldRequesterId = relationship.RequesterUserId;

                finalRelationship = _userMapper.ToFriendship(requesterId, oldRequesterId);
                await _friendshipRepository.AddAsync(finalRelationship);
            }
            else
            {
                await _friendshipRepository.UpdateStatusAsync(relationship, FriendshipStatus.Pending);
                relationship.SentAt = DateTime.UtcNow;

                finalRelationship = relationship;
                await _friendshipRepository.SaveChangesAsync();
            }
            
            var friendshipDto = _friendshipMapper.ToMinimalDisplay(finalRelationship);
            response.Ok(friendshipDto, ResponseMessages.FriendRequestSent);
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
        var response = new ListResponse<StandardFriendshipDto>();
        
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
            response.Ok(friendshipDtos, ResponseMessages.ResourceRetrieved(friendshipTypeMessage));
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }
        return response;
    }
    
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
            response.Ok(relationshipDto, ResponseMessages.RelationshipFetched);
            await LogResultAsync(response.Succeeded, nameof(GetRelationshipAsync), nameof(Friendship), $"{response.Message} in standard public form.", null);
        }
        else
        {
            response.Fail(ResponseMessages.RelationshipNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetRelationshipAsync), nameof(Friendship), $"Relationship retrieval failed. {response.Message}", null);
        }

        return response;
    }
    public async Task<ApplicationResponse<AcceptedFriendshipDto>> GetAcceptedFriendshipAsync(int currentUserId, int otherUserId)
    {
        var response   = new ApplicationResponse<AcceptedFriendshipDto>();
        
        var friendship = await _friendshipRepository.GetAcceptedFriendshipAsync(currentUserId, otherUserId, false);

        if (friendship is not null)
        {
            var otherUser = friendship.RequesterUserId == otherUserId
                ? friendship.RequesterUser
                : friendship.AddresseeUser;
            
            var friendshipDto = _friendshipMapper.ToAcceptedDisplay(friendship, otherUser!);
            response.Ok(friendshipDto, ResponseMessages.RelationshipFetched);           
            await LogResultAsync(response.Succeeded, nameof(GetAcceptedFriendshipAsync), nameof(Friendship), $"{response.Message}", null);
        }
        else
        {
            response.Fail(ResponseMessages.FriendshipNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetAcceptedFriendshipAsync), nameof(Friendship), response.Message, null);
        }

        return response;
    }
    
    public async Task<ApplicationResponse<StandardFriendshipDto>> RespondToRequestAsync(int requesterId, int addresseeId, FriendshipStatus status)
    {
        var response = new ApplicationResponse<StandardFriendshipDto>();

        if (ValidRequestResponse(status))
        {
            var friendship = await _friendshipRepository.GetRelationshipAsync(requesterId, addresseeId, true);

            if (friendship is not null && friendship.FriendshipStatus == FriendshipStatus.Pending)
            {
                await _friendshipRepository.UpdateStatusAsync(friendship, status);
                
                var friendshipDto = _friendshipMapper.ToStandardDisplay(friendship, friendship.RequesterUser!);
                await _friendshipRepository.SaveChangesAsync();
                
                response.Ok(friendshipDto, ResponseMessages.ResponseSent);
                await LogResultAsync(response.Succeeded, nameof(RespondToRequestAsync), nameof(Friendship), "Response written to the database", null);
            }
            else
            {
                response.Fail(ResponseMessages.PendingRequestNotFound);
                await LogResultAsync(response.Succeeded, nameof(RespondToRequestAsync), nameof(Friendship), $"Could not respond to a request. {ResponseMessages.PendingRequestNotFound}", null);
            }
        }

        return response;
    }
    private bool ValidRequestResponse(FriendshipStatus status) =>
        status is FriendshipStatus.Accepted or FriendshipStatus.Declined;
    
    public async Task<ApplicationResponse> RemoveRelationshipAsync(int userId, int friendId)
    {
        var response = new ApplicationResponse();
        
        var callerExists = await _userRepository.ExistsByIdAsync(userId);

        if (callerExists)
        {
            var friendship = await _friendshipRepository.GetRelationshipAsync(userId, friendId, false);

            if (friendship is not null)
            {
                await _messageRepository.DeleteConversationAsync(userId, friendId);
                await _friendshipRepository.DeleteAsync(friendship);
            
                response.Ok(ResponseMessages.RelationshipRemoved);
                await LogResultAsync(response.Succeeded, nameof(RemoveRelationshipAsync), nameof(Friendship), $"{response.Message} from the database", null);
            }
            else
            {
                response.Fail(ResponseMessages.RelationshipNotFound);
                await LogResultAsync(response.Succeeded, nameof(RemoveRelationshipAsync), nameof(Friendship), $"Could not remove relationship. {response.Message}", null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.InvalidRequest);
            await LogResultAsync(response.Succeeded, nameof(RemoveRelationshipAsync), nameof(Friendship), $"Caller data not resolved", null);
        }

        return response;
    }
}
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.Users.Friends;

namespace aspnetproject.Infrastructure.Mappers;

public static class UserMapper 
{
    
    public static User ToRegisteredEntity(RegisterDto registerDto, string passwordHash, string passwordSalt)
    {
        return new User
        {
            Username = registerDto.Username.ToLower(),
            Email = registerDto.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Bio = registerDto.Bio,
            DateOfBirth = registerDto.DateOfBirth
        };
    }
    public static MinimalUserDto ToMinimalDisplay(User user)
    {
        return new MinimalUserDto
        {
            Id = user.Id,
            Username = user.Username,
        };
    }

    public static StandardUserDto ToStandardDisplay(User user)
    {
        return new StandardUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Bio = user.Bio,

            RegisteredAt = user.CreatedAt,
            DateOfBirth = user.DateOfBirth
        };
    }

    public static FullUserDto ToFullDisplay(User user)
    {
        return new FullUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            Roles = user.UserRoles.Select(userRole => userRole.Role).Select(role => role!.Name).ToList(),
            RegisteredAt = user.CreatedAt,
            DateOfBirth = user.DateOfBirth
        };
    }

    public static AdminUserDto ToAdminDisplay(User user)
    {
        return new AdminUserDto
        {
            Id              = user.Id,
            Username        = user.Username,
            Email           = user.Email,
            IsAdministrator = user.UserRoles.Any(userRole => userRole.Role!.Name == nameof(RoleName.Administrator)),
            State           = ToAccountState(user),

            RegisteredAt  = user.CreatedAt,
            DeactivatedAt = user.AccountDeactivatedAt,
            DeletedAt     = user.AccountDeletedAt
        };
    }

    // Soft deletion wins over deactivation, matching UserRepository.GetAccountsAsync's state filter.
    private static AccountState ToAccountState(User user)
    {
        var state = AccountState.Active;

        bool isSoftDeleted = user.AccountDeletedAt is not null;
        bool isDeactivated = user.AccountDeactivatedAt is not null;

        if (isSoftDeleted)
        {
            state = AccountState.SoftDeleted;
        }
        else if (isDeactivated)
        {
            state = AccountState.Deactivated;
        }

        return state;
    }

    public static ConversationFriendDto ToConversationFriendDisplay(ConversationFriendProjection friend)
    {
        return new ConversationFriendDto
        {
            ConversationFriend = new DisplayFriendDto
            {
                Id = friend.FriendId,
                Username = friend.IsDeleted ? ResponseMessages.AccountDeletedUsername : friend.IsDeactivated ? ResponseMessages.AccountDeactivatedUsername : friend.FriendUsername,
                LastActiveAt = friend.FriendLastActiveAt,
            },
            
            LastMessageContent = friend.LastMessageContent,
            LastMessageSenderId = friend.LastMessageSenderId,
            LastMessageSentAt = friend.LastMessageSentAt,
            LastMessageImageCount = friend.LastMessageImageCount,

            UnreadCount = friend.UnreadCount
        };
    }

    public static DisplayFriendDto ToFriendDisplay(User nonConversationFriend)
    {
        return new DisplayFriendDto
        {
            Id = nonConversationFriend.Id,
            Username = nonConversationFriend.Username,
            LastActiveAt = nonConversationFriend.LastOnlineAt,
        };
    }

    public static Friendship ToFriendship(int requesterId, int addresseeId)
    {
        return new Friendship
        {
            RequesterUserId = requesterId,
            AddresseeUserId = addresseeId,
        };
    }
}
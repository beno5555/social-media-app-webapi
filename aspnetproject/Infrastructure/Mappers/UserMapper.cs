using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class UserMapper 
{
    public User ToEntity(RegisterDto registerDto, string passwordHash, string passwordSalt)
    {
        return new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Bio = registerDto.Bio,
            DateOfBirth = registerDto.DateOfBirth
        };
    }

    public MinimalUserDto ToMinimalDisplay(User user)
    {
        return new MinimalUserDto
        {
            Id = user.Id,
            Username = user.Username,
        };
    }

    public StandardUserDto ToStandardDisplay(User user)
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

    public ConversationFriendDto ToConversationFriendDisplay(ConversationFriendProjection friend)
    {
        return new ConversationFriendDto
        {
            Friend = new MinimalUserDto
            {
                Id = friend.FriendId,
                Username = friend.FriendUsername
            },
            LastMessageContent = friend.LastMessageContent,
            LastMessageSenderId = friend.LastMessageSenderId,
            LastMessageSentAt = friend.LastMessageSentAt,
            
            UnreadCount = friend.UnreadCount
        };
    }

    public Friendship ToFriendship(int requesterId, int addresseeId)
    {
        return new Friendship
        {
            RequesterUserId = requesterId,
            AddresseeUserId = addresseeId,
        };
    }
}
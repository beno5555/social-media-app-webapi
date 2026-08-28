using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Models;

namespace aspnetproject.Infrastructure.Mappers;

public class UserMapper 
{
    public User ToEntity(CreateAccountDto registerDto, string passwordHash, string passwordSalt)
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

    public FullUserDto ToFullDisplay(User user)
    {
        return new FullUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
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
using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.Users.Friends;

namespace aspnetproject.Infrastructure.Mappers;

public static class FriendshipMapper
{
    /// <summary>
    /// assumes not null User foreign keys
    /// </summary>
    public static MinimalFriendshipDto ToMinimalDisplay(Friendship friendship)
    {
        return new MinimalFriendshipDto
        {
            AddresseeId = friendship.AddresseeUserId,
            RequesterId = friendship.RequesterUserId,
            SentAt = friendship.SentAt,
            Status = friendship.FriendshipStatus.ToString()
        };
    }

    public static StandardFriendshipDto ToStandardDisplay(Friendship friendship, int currentUserId)
    {
        (User? otherUser, int? otherUserId) = friendship.AddresseeUserId == currentUserId
            ? (friendship.RequesterUser, friendship.RequesterUserId)
            : (friendship.AddresseeUser, friendship.AddresseeUserId);
        
        return new StandardFriendshipDto
        {
            OtherUser = new MinimalUserDto
            {
                Id = otherUserId,
                Username = UsernameMapper.ResolveUsername(otherUser, otherUserId)
            },
            
            Status =  friendship.FriendshipStatus.ToString(),
            SentAt = friendship.SentAt,
            LastUpdatedAt = friendship.LastUpdatedAt,
        };
    }
    
    public static AcceptedFriendshipDto ToAcceptedDisplay(Friendship friendship, User otherUser)
    {
        return new AcceptedFriendshipDto
        {
            OtherUser = new DisplayFriendDto
            {
                Id = otherUser.Id,
                Username = otherUser.Username,
                LastActiveAt =  otherUser.LastOnlineAt,
            },
            
            Status =  friendship.FriendshipStatus.ToString(),
            SentAt = friendship.SentAt,
            LastUpdatedAt = friendship.LastUpdatedAt,
        };
    }
}
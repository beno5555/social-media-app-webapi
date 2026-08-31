using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Friendships;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.Users.Friends;

namespace aspnetproject.Infrastructure.Mappers;

public static class FriendshipMapper
{
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

    public static StandardFriendshipDto ToStandardDisplay(Friendship friendship, User otherUser)
    {
        return new StandardFriendshipDto
        {
            OtherUser = new MinimalUserDto
            {
                Id = otherUser.Id,
                Username = otherUser.Username
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
                LastActiveAt =  otherUser.LastActiveAt,
            },
            
            Status =  friendship.FriendshipStatus.ToString(),
            SentAt = friendship.SentAt,
            LastUpdatedAt = friendship.LastUpdatedAt,
        };
    }
}
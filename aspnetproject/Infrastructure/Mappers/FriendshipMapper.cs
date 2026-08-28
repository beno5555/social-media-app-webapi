using aspnetproject.Common.Dtos.Friendships;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class FriendshipMapper
{
    public MinimalFriendshipDto ToMinimalDisplay(Friendship friendship)
    {
        return new MinimalFriendshipDto
        {
            AddresseeId = friendship.AddresseeUserId,
            RequesterId = friendship.RequesterUserId,
            SentAt = friendship.SentAt,
            Status = friendship.FriendshipStatus.ToString()
        };
    }

    public StandardFriendshipDto ToStandardDisplay(Friendship friendship, User otherUser)
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
}
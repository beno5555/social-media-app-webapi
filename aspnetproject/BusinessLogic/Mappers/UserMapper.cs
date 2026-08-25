using aspnetproject.BusinessLogic.Dtos;
using aspnetproject.BusinessLogic.Dtos.UserDtos;
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

    public DisplayUserDto ToDisplay(User user)
    {
        return new DisplayUserDto(
            user.Id,
            user.Username,
            user.Bio,
            user.CreatedAt,
            user.DateOfBirth
            );
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
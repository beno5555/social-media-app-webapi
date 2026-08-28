using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;
using Microsoft.IdentityModel.Tokens;

namespace aspnetproject.Infrastructure.Mappers;

public class AuthMapper
{
    public User ToRegisteredEntity(RegisterDto registerDto, string passwordHash, string passwordSalt)
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
}
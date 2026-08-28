namespace aspnetproject.BusinessLogic.Dtos.UserDtos;

public record RegisterDto(string Username, string Email, string Password, DateTime DateOfBirth, string? Bio = null);
namespace aspnetproject.BusinessLogic.Dtos.UserDtos;

public record DisplayUserDto(int Id, string Username, string? Bio, DateTime CreatedAt, DateTime DateOfBirth);
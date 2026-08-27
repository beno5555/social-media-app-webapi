using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace aspnetproject.Common.Dtos.Users;

public class StandardUserDto
{
    public int      Id           { get; set; }
    public string   Username     { get; set; } = string.Empty;
    public string?  Bio          { get; set; }
    
    public DateTime RegisteredAt { get; set; }
    public DateTime DateOfBirth  { get; set; }
}
namespace aspnetproject.BusinessLogic.Dtos;

public class SessionUser
{
    public int    UserId     { get; set; } 
    public string Username   { get; set; } = string.Empty;
    public bool   IsLoggedIn => UserId != 0;
    
}
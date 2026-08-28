namespace aspnetproject.ProjectConstants;

public static class Constants
{
    public const int EmailMinLength    = 10;
    public const int EmailMaxLength    = 100;
    public const int UsernameMinLength = 3;
    public const int UsernameMaxlength = EmailMaxLength;
    
    public const int PasswordMinLength     = 6;
    public const int PasswordMaxLength     = 100;
    public const int PasswordHashMaxLength = 44;
    public const int PasswordSaltMaxLength = 44;
    
    public const int BioMaxLength                = 300;
    public const int PostTitleMaxLength          = 100;
    public const int PostContentMaxLength        = 3000;
    public const int PostContentPreviewLength    = 50;
    public const int CommentMaxLength            = 500;
    public const int CommentContentPreviewLength = 70;
    public const int MessageMaxLength            = 1000;
    
    public const int MinAge = 13;
    public const int MaxAge = 130;

    public const int    DefaultPageSize             = 10;
    public const int    DefaultConversationPageSize = 10;

    public static readonly TimeSpan EditMessageWindow = TimeSpan.FromMinutes(20);

    public const int  ChatWidth  = 80;
    public const char ChatBorder = '|';
    
    public const double OwnMessageIndentPercent     = 0.3;
    public const double OtherMessageMaxWidthPercent = 0.6;
    
    public const string EmailRegexPattern           = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string UsernameRegexPattern        = "^(?=.*[a-zA-Z])[a-zA-Z0-9._-]+$";

    public const  int MenuBackTrackDelayInMilliseconds = 800;

    public const ConsoleColor OwnMessageColor     = ConsoleColor.Cyan;
    public const ConsoleColor OtherMessageColor   = ConsoleColor.Green;
    public const ConsoleColor TimestampColor      = ConsoleColor.DarkGray;
    public const ConsoleColor MessageContentColor = ConsoleColor.White;

    public const ConsoleKey HomeKey     = ConsoleKey.Escape;
    public const string     HomeCommand = "exit";
}
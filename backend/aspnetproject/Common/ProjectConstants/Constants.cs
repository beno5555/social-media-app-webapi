namespace aspnetproject.Common.ProjectConstants;

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

    public const int BioMinLength         = 1;
    public const int BioMaxLength         = 300;
    public const int PostTitleMaxLength   = 100;
    public const int PostContentMaxLength = 3000;
    public const int CommentMaxLength     = 500;
    public const int MessageMaxLength     = 1000;

    public const int    BytesPerMegabyte      = 1024 * 1024;
    public const int    ImageAddressMaxLength = 200;
    public const string ImagesRequestPath     = "/api/images";

    // Room in an upload request for its text fields and multipart boundaries, on top of the images themselves.
    public const int MultipartTextAllowanceBytes = BytesPerMegabyte;
    
    public const int MinAge                     = 13;
    public const int MaxAge                     = 100;
    public const int UsernameChangeCooldownDays = 20;
    
    public const int DefaultPageSize = 10;
    public const int MaxPageSize     = 100;

    // Feed score = (connection weight + likes + comments) / (age in hours + FeedAgeOffsetHours) ^ FeedAgeGravity.
    // A connection weight counts as that many likes or comments. Every weight is above zero, so a post nobody
    // has reacted to still loses score as it ages. The offset keeps a brand-new post from dividing by zero,
    // and a higher gravity makes posts sink faster. With these values a friend's post nobody has reacted to
    // stays above a brand-new stranger's post for about 12 hours, and a mutual friend's for about 4.
    public const double FeedFriendWeight       = 20;
    public const double FeedMutualFriendWeight = 5;
    public const double FeedOtherUserWeight    = 1;
    public const double FeedAgeOffsetHours     = 2;
    public const double FeedAgeGravity         = 1.5;
    public const double MinutesPerHour         = 60;

    public const int LogDetailsMaxLength    = 1000;
    public const int LogActionMaxLength     = 100;
    public const int LogEntityNameMaxLength = 50;

    public static readonly TimeSpan EditMessageWindow = TimeSpan.FromMinutes(20);

    public const string EmailRegexPattern    = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string UsernameRegexPattern = "^(?=.*[a-zA-Z])[a-zA-Z0-9._-]+$";
}
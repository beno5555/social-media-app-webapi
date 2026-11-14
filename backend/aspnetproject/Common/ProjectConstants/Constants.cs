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
    
    public const int FirstPageNumber = 1;
    public const int MinPageSize     = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize     = 100;

    // Up to this page, the offset (page - 1) * MaxPageSize stays inside an int.
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public const double MinutesPerHour = 60;

    public const int LogDetailsMaxLength    = 1000;
    public const int LogActionMaxLength     = 100;
    public const int LogEntityNameMaxLength = 50;

    public static readonly TimeSpan EditMessageWindow = TimeSpan.FromMinutes(20);

    public const string EmailRegexPattern    = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string UsernameRegexPattern = "^(?=.*[a-zA-Z])[a-zA-Z0-9._-]+$";
}
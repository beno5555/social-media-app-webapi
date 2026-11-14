using aspnetproject.Common.ProjectConstants;

namespace aspnetproject.Infrastructure.Services.Storage.Configuration;

public class ImageStorageConfiguration
{
    // Relative paths resolve against the content root (the folder that holds aspnetproject.csproj).
    public string UploadFolder        { get; set; } = "Uploads";
    public long   MaxFileSizeBytes    { get; set; } = 5 * Constants.BytesPerMegabyte;
    public int    MaxImagesPerPost    { get; set; } = 10;
    public int    MaxImagesPerMessage { get; set; } = 5;
}

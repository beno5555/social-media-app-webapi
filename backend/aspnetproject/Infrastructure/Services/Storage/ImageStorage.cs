using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Services.Logging;
using aspnetproject.Infrastructure.Services.Storage.Configuration;
using Microsoft.Extensions.Options;

namespace aspnetproject.Infrastructure.Services.Storage;

/// <summary>
/// The only code that touches uploaded files: it checks uploads, saves them under server-made names, deletes them and
/// turns a stored file into its address. Moving images to cloud storage means changing this class only.
/// </summary>
public class ImageStorage
{
    private const string JpegExtension = ".jpg";
    private const string PngExtension  = ".png";
    private const string WebPExtension = ".webp";

    private static readonly byte[] JpegSignature       = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature        = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature       = "RIFF"u8.ToArray();
    private static readonly byte[] WebPSignature       = "WEBP"u8.ToArray();
    private const           int    WebPSignatureOffset = 8;
    private const           int    HeaderLength        = 12;

    private const string FileNameFormat = "N";

    private readonly ImageStorageConfiguration _configuration;
    private readonly SystemLogger              _systemLogger;

    public string UploadFolderPath { get; }

    public ImageStorage(IOptions<ImageStorageConfiguration> options, IWebHostEnvironment environment, SystemLogger systemLogger)
    {
        _configuration   = options.Value;
        _systemLogger    = systemLogger;
        UploadFolderPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _configuration.UploadFolder));
    }

    public async Task<ApplicationResponse<List<CheckedImage>>> CheckPostImagesAsync(IReadOnlyList<IFormFile> files)
    {
        int maxImageCount = _configuration.MaxImagesPerPost;

        return await CheckWithinCountAsync(files, maxImageCount, ResponseMessages.TooManyPostImages(maxImageCount));
    }

    public async Task<ApplicationResponse<List<CheckedImage>>> CheckMessageImagesAsync(IReadOnlyList<IFormFile> files)
    {
        int maxImageCount = _configuration.MaxImagesPerMessage;

        return await CheckWithinCountAsync(files, maxImageCount, ResponseMessages.TooManyMessageImages(maxImageCount));
    }

    /// <summary>Saves checked images and returns their addresses in the same order. On failure, nothing is left behind.</summary>
    public async Task<List<string>> SaveAsync(IReadOnlyList<CheckedImage> images)
    {
        var savedFileNames = new List<string>();
        Directory.CreateDirectory(UploadFolderPath);

        try
        {
            foreach (var image in images)
            {
                string fileName = $"{Guid.NewGuid().ToString(FileNameFormat)}{image.Extension}";
                savedFileNames.Add(fileName);

                await using var fileStream = new FileStream(Path.Combine(UploadFolderPath, fileName), FileMode.CreateNew);
                await image.File.CopyToAsync(fileStream);
            }
        }
        catch (Exception)
        {
            await DeleteFilesAsync(savedFileNames);
            throw;
        }

        return savedFileNames.Select(ToAddress).ToList();
    }

    /// <summary>Best effort: a file that cannot be removed is logged and does not fail the caller.</summary>
    public async Task DeleteAsync(IEnumerable<string> addresses)
    {
        // Only the file name of an address is used, so an address can never point outside the upload folder.
        await DeleteFilesAsync(addresses.Select(Path.GetFileName).OfType<string>());
    }

    private string ToAddress(string fileName)
    {
        return $"{Constants.ImagesRequestPath}/{fileName}";
    }

    private async Task<ApplicationResponse<List<CheckedImage>>> CheckWithinCountAsync(
        IReadOnlyList<IFormFile> files, int maxImageCount, string tooManyImagesMessage)
    {
        var response = new ApplicationResponse<List<CheckedImage>>();

        bool isWithinCountLimit = files.Count <= maxImageCount;
        if (isWithinCountLimit)
        {
            response = await CheckEachAsync(files);
        }
        else
        {
            response.Fail(tooManyImagesMessage);
        }

        return response;
    }

    private async Task<ApplicationResponse<List<CheckedImage>>> CheckEachAsync(IReadOnlyList<IFormFile> files)
    {
        var    response      = new ApplicationResponse<List<CheckedImage>>();
        var    checkedImages = new List<CheckedImage>();
        string? failure      = null;

        for (int index = 0; index < files.Count && failure is null; index++)
        {
            var  file              = files[index];
            bool isWithinSizeLimit = file.Length <= _configuration.MaxFileSizeBytes;

            if (isWithinSizeLimit)
            {
                string? extension = await DetectExtensionAsync(file);
                if (extension is not null)
                {
                    checkedImages.Add(new CheckedImage(file, extension));
                }
                else
                {
                    failure = ResponseMessages.ImageFormatNotAccepted;
                }
            }
            else
            {
                failure = ResponseMessages.ImageTooLarge(_configuration.MaxFileSizeBytes / (double)Constants.BytesPerMegabyte);
            }
        }

        if (failure is null)
        {
            response.Ok(checkedImages);
        }
        else
        {
            response.Fail(failure);
        }

        return response;
    }

    // The format comes from the file's leading bytes. Its name and declared content type are never trusted.
    private async Task<string?> DetectExtensionAsync(IFormFile file)
    {
        string? extension = null;

        var header = new byte[HeaderLength];
        await using var stream = file.OpenReadStream();
        int bytesRead = await stream.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false);
        var leadingBytes = header.AsSpan(0, bytesRead);

        bool isJpeg = leadingBytes.StartsWith(JpegSignature);
        bool isPng  = leadingBytes.StartsWith(PngSignature);
        bool isWebP = bytesRead == HeaderLength
                      && leadingBytes.StartsWith(RiffSignature)
                      && leadingBytes[WebPSignatureOffset..].StartsWith(WebPSignature);

        if (isJpeg)
        {
            extension = JpegExtension;
        }
        else if (isPng)
        {
            extension = PngExtension;
        }
        else if (isWebP)
        {
            extension = WebPExtension;
        }

        return extension;
    }

    private async Task DeleteFilesAsync(IEnumerable<string> fileNames)
    {
        foreach (string fileName in fileNames)
        {
            try
            {
                File.Delete(Path.Combine(UploadFolderPath, fileName));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                await _systemLogger.LoggingErrorAsync(exception, $"Could not delete stored image {fileName}");
            }
        }
    }
}

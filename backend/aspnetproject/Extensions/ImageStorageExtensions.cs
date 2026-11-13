using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Services.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace aspnetproject.Extensions;

public static class ImageStorageExtensions
{
    private const string NoSniff = "nosniff";

    /// <summary>
    /// Serves stored images as static files under /api/images, so they cross the same proxy path as the API (ADR 0001).
    /// Anyone may fetch them: an image tag cannot send the in-memory access token.
    /// </summary>
    public static void UseImageStorage(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        string uploadFolderPath = scope.ServiceProvider.GetRequiredService<ImageStorage>().UploadFolderPath;
        Directory.CreateDirectory(uploadFolderPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadFolderPath),
            RequestPath  = Constants.ImagesRequestPath,
            OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.XContentTypeOptions] = NoSniff
        });
    }
}

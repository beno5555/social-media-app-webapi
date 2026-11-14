using aspnetproject.Infrastructure.Services.Storage.Configuration;
using Microsoft.Extensions.Options;

namespace aspnetproject.Controllers.Filters;

// The server's default request size (30 MB) is smaller than ten 5 MB photos, so a post upload raises it.
public class PostUploadRequestSizeFilter : ImageUploadRequestSizeFilter
{
    public PostUploadRequestSizeFilter(IOptions<ImageStorageConfiguration> options)
        : base(options.Value.MaxFileSizeBytes, options.Value.MaxImagesPerPost)
    {
    }
}

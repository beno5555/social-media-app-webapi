using aspnetproject.Infrastructure.Services.Storage.Configuration;
using Microsoft.Extensions.Options;

namespace aspnetproject.Controllers.Filters;

// Sized from the configured limits, so five 5 MB photos fit by default and raising a limit raises this one too.
public class MessageUploadRequestSizeFilter : ImageUploadRequestSizeFilter
{
    public MessageUploadRequestSizeFilter(IOptions<ImageStorageConfiguration> options)
        : base(options.Value.MaxFileSizeBytes, options.Value.MaxImagesPerMessage)
    {
    }
}

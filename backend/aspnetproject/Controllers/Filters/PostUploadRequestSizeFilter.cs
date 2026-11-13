using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Services.Storage.Configuration;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace aspnetproject.Controllers.Filters;

/// <summary>
/// Raises the request size limit of a post upload to fit the configured image limits. The server's default (30 MB)
/// is smaller than ten 5 MB photos. Runs before model binding, so the body has not been read yet.
/// </summary>
public class PostUploadRequestSizeFilter : IResourceFilter
{
    private readonly long _requestSizeLimitBytes;

    public PostUploadRequestSizeFilter(IOptions<ImageStorageConfiguration> options)
    {
        var configuration = options.Value;
        _requestSizeLimitBytes = configuration.MaxFileSizeBytes * configuration.MaxImagesPerPost + Constants.MultipartTextAllowanceBytes;
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var features = context.HttpContext.Features;

        var  bodySizeFeature = features.Get<IHttpMaxRequestBodySizeFeature>();
        bool canRaiseLimit   = bodySizeFeature is { IsReadOnly: false };
        if (canRaiseLimit)
        {
            bodySizeFeature!.MaxRequestBodySize = _requestSizeLimitBytes;
        }

        bool formNotYetRead = features.Get<IFormFeature>()?.Form is null;
        if (formNotYetRead)
        {
            var formOptions = new FormOptions { MultipartBodyLengthLimit = _requestSizeLimitBytes };
            features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, formOptions));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}

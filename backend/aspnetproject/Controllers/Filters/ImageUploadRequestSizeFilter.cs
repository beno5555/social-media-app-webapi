using aspnetproject.Common.ProjectConstants;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace aspnetproject.Controllers.Filters;

/// <summary>
/// Sets the request size limit of an upload to fit its configured image limits: every image at the size limit, plus room
/// for the text fields. Runs before model binding, so the body has not been read yet.
/// </summary>
public abstract class ImageUploadRequestSizeFilter : IResourceFilter
{
    private readonly long _requestSizeLimitBytes;

    protected ImageUploadRequestSizeFilter(long maxFileSizeBytes, int maxImageCount)
    {
        _requestSizeLimitBytes = maxFileSizeBytes * maxImageCount + Constants.MultipartTextAllowanceBytes;
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

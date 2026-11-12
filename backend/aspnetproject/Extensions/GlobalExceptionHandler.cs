using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Extensions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandler(IWebHostEnvironment env
    )
    {
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var systemLogger = httpContext.RequestServices.GetRequiredService<SystemLogger>();
        await systemLogger.LogEndpointErrorAsync(exception, httpContext.Request.Method, httpContext.Request.Path);

        if (exception is DbUpdateException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        var response = new ApplicationResponse();

        if (_env.IsDevelopment())
        {
            var detail = ResponseMessages.ExceptionDetail(exception.GetType().Name, exception.Message);

            var innerMessage    = exception.InnerException?.Message;
            bool hasInnerDetail = !string.IsNullOrEmpty(innerMessage);
            if (hasInnerDetail)
            {
                detail = ResponseMessages.WithInnerExceptionDetail(detail, innerMessage!);
            }

            response.Fail(detail);
        }
        else
        {
            response.Fail(ResponseMessages.UnexpectedError);
        }

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
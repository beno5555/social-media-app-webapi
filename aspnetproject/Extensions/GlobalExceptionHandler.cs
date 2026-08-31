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

        httpContext.Response.ContentType = "application/json";

        if (_env.IsDevelopment())
        {
            var body = new
            {
                error = exception.Message,
                stackTrace = exception.StackTrace
            };

            await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}", cancellationToken);
        }

        return true;
    }
}
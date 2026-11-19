using LibraryManagementAPI.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace LibraryManagementAPI.Infrastructure;

/// <summary>
/// Turns any unhandled exception into a 500 response in the standard <see cref="ApiResponse{T}"/> shape,
/// and logs the details instead of leaking them to the caller.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var errors = _environment.IsDevelopment() ? new[] { exception.Message } : null;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.FailureResponse("An unexpected error occurred.", errors), cancellationToken);
        return true;
    }
}

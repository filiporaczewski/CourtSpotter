using Microsoft.AspNetCore.Diagnostics;

namespace CourtSpotter.AspNetCore.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        
        var result = Results.Problem(
            title: "An error occurred",
            detail: "An internal server error occurred. Please try again later.",
            statusCode: StatusCodes.Status500InternalServerError,
            instance: httpContext.Request.Path
        );
        
        await result.ExecuteAsync(httpContext);
        return true;
    }
}
using Microsoft.AspNetCore.Diagnostics;

namespace SOFIA.API.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Reuse .NET's TraceIdentifier as the correlation id for this request
        var traceId = httpContext.TraceIdentifier;

        // Log the error with the TraceId so it can be correlated in log search
        logger.LogError(
            exception,
            "Unhandled exception. [TraceId: {TraceId}]",
            traceId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error del Servidor",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Detail = $"An unexpected error occurred. Please contact support if the problem persists. Trace ID: {traceId}",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        // Also surface the TraceId as an extra JSON property for API consumers
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Domain.Common;

namespace SOFIA.API.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public const string ConcurrencyConflictCode = "Concurrency.Conflict";
    public const string RequestTooLargeCode = "Request.TooLarge";
    public const string RequestInvalidCode = "Request.Invalid";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Reuse .NET's TraceIdentifier as the correlation id for this request
        var traceId = httpContext.TraceIdentifier;

        if (exception is DbUpdateConcurrencyException)
        {
            return await WriteConcurrencyConflictAsync(httpContext, traceId, cancellationToken);
        }

        if (exception is ExcelImportException excelImport)
        {
            return await WriteErrorAsync(httpContext, excelImport.Error, StatusCodes.Status400BadRequest, traceId, cancellationToken);
        }

        // Kestrel raises this when a body exceeds RequestSizeLimit or cannot be read; it is a client error, not a 500
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning("Rejected request body on {Method} {Path} with {StatusCode}. [TraceId: {TraceId}]", httpContext.Request.Method, httpContext.Request.Path, badRequest.StatusCode, traceId);
            var error = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? Error.Validation(RequestTooLargeCode, "The request body exceeds the allowed size.")
                : Error.Validation(RequestInvalidCode, "The request body could not be read.");
            return await WriteErrorAsync(httpContext, error, badRequest.StatusCode, traceId, cancellationToken);
        }

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

    // A stale RowVersion means another request changed the same row first; same body shape as a business-rule conflict
    private async Task<bool> WriteConcurrencyConflictAsync(HttpContext httpContext, string traceId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Concurrency conflict on {Method} {Path}. [TraceId: {TraceId}]", httpContext.Request.Method, httpContext.Request.Path, traceId);

        var error = Error.Conflict(ConcurrencyConflictCode, "The record was changed by another operation. Reload it and try again.");
        return await WriteErrorAsync(httpContext, error, StatusCodes.Status409Conflict, traceId, cancellationToken);
    }

    private static async Task<bool> WriteErrorAsync(HttpContext httpContext, Error error, int statusCode, string traceId, CancellationToken cancellationToken)
    {
        var problemDetails = (ProblemDetails)Result.Failure(error, statusCode).ToProblemResult().Value!;
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

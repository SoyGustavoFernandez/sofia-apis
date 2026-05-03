using Microsoft.AspNetCore.Diagnostics;

namespace SOFIA.API.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Generamos un TraceId único para esta petición (o usamos el de .NET)
        var traceId = httpContext.TraceIdentifier;

        // Loggeamos el error incluyendo el TraceId para búsqueda rápida
        logger.LogError(
            exception,
            "Ocurrió una excepción no controlada. [TraceId: {TraceId}]",
            traceId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error del Servidor",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Detail = $"Ocurrió un error inesperado. Por favor, contacte a soporte si el problema persiste. Código de rastreo: {traceId}",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        // Agregamos el TraceId también como una propiedad extra en el JSON
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

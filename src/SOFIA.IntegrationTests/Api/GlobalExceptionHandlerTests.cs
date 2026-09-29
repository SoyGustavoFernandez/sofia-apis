using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SOFIA.API.Infrastructure;
using SOFIA.Application.Common.Excel;
using SOFIA.Domain.Common;

namespace SOFIA.IntegrationTests.Api;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_ShouldReturnConflictWithStableCode_WhenRowVersionIsStale()
    {
        var context = NewContext();

        var handled = await _handler.TryHandleAsync(context, new DbUpdateConcurrencyException("stale RowVersion"), CancellationToken.None);

        _ = handled.Should().BeTrue();
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        using var body = await ReadBodyAsync(context);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be("Concurrency.Conflict", because: "the SPA maps error.code to an i18n key");
        _ = body.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        _ = body.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequestWithCode_WhenExcelUploadIsRejected()
    {
        var context = NewContext();
        var exception = new ExcelImportException(Error.Validation(ExcelImportException.FilasExcedidas, "too many rows"));

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        _ = handled.Should().BeTrue();
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        using var body = await ReadBodyAsync(context);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(ExcelImportException.FilasExcedidas);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnPayloadTooLargeWithCode_WhenBodyExceedsLimit()
    {
        var context = NewContext();
        var exception = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        _ = handled.Should().BeTrue();
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        using var body = await ReadBodyAsync(context);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(GlobalExceptionHandler.RequestTooLargeCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnServerError_WhenExceptionIsUnexpected()
    {
        var context = NewContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        _ = handled.Should().BeTrue();
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        using var body = await ReadBodyAsync(context);
        _ = body.RootElement.TryGetProperty("code", out _).Should().BeFalse();
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Put;
        context.Request.Path = "/api/v1/ventas/1/completar";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}

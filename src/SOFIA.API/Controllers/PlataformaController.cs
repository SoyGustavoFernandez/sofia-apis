using Microsoft.AspNetCore.RateLimiting;
using SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;
using SOFIA.Application.Plataforma.Queries.GetConfiguracionPago;
using SOFIA.Application.Plataforma.Queries.GetDashboardPlataforma;
using SOFIA.Application.Security;
using SOFIA.Domain.Entities;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PlataformaController(ISender sender) : ControllerBase
{
    // Leaves room for the form fields around a QR image of the maximum allowed size
    private const int MaxRequestBytes = PlataformaConfiguracionPago.MaxQrBytes + (64 * 1024);

    /// <summary>Payment details shown on the login page to companies whose subscription is not active, hence anonymous.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.PublicInfoPolicy)]
    [HttpGet("pago")]
    public async Task<IActionResult> GetConfiguracionPago(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetConfiguracionPagoQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = PlataformaAdminPolicy.Name)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTimeOffset? inicioMes, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardPlataformaQuery(inicioMes), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = PlataformaAdminPolicy.Name)]
    [RequestSizeLimit(MaxRequestBytes)]
    [HttpPut("pago")]
    public async Task<IActionResult> GuardarConfiguracionPago([FromForm] ConfiguracionPagoRequest request, CancellationToken cancellationToken)
    {
        byte[]? qrImagen = null;
        if (request.Qr is { Length: > 0 })
        {
            if (request.Qr.Length > PlataformaConfiguracionPago.MaxQrBytes)
            {
                return BadRequest(new ProblemDetails { Status = 400, Extensions = { ["code"] = "ConfiguracionPago.Qr.Tamano" } });
            }

            using var stream = new MemoryStream();
            await request.Qr.CopyToAsync(stream, cancellationToken);
            qrImagen = stream.ToArray();
        }

        var result = await sender.Send(new GuardarConfiguracionPagoCommand
        {
            WhatsappNumero = request.WhatsappNumero ?? string.Empty,
            YapeNumero = request.YapeNumero,
            Instrucciones = request.Instrucciones,
            QrImagen = qrImagen
        }, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemResult();
    }

    public record ConfiguracionPagoRequest(string? WhatsappNumero, string? YapeNumero, string? Instrucciones, IFormFile? Qr);
}

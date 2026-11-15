using SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;
using SOFIA.Application.Devoluciones.Queries.GetDevolucionById;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DevolucionesController(ISender sender) : ControllerBase
{

    [HasPermission("Devoluciones", "ProcesarDevolucion")]
    [HttpPost]
    public async Task<IActionResult> ProcesarDevolucion([FromBody] ProcesarDevolucionCommand request)
    {
        var result = await sender.Send(request);
        return result.ToActionResult();
    }

    [HasPermission("Devoluciones", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetDevoluciones([FromQuery] GetDevolucionesQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Devoluciones", "Leer")]
    public async Task<IActionResult> GetDevolucionById(Guid id)
    {
        var result = await sender.Send(new GetDevolucionByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Devoluciones", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] DevolucionExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDevolucionesQuery
        {
            EmpleadoAutorizaId = request.EmpleadoAutorizaId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            MetodoReembolso = request.MetodoReembolso,
            Destino = request.Destino,
            MotivoSunatCatalogo = request.MotivoSunatCatalogo,
            NumeroNotaCredito = request.NumeroNotaCredito,
            MontoMin = request.MontoMin,
            MontoMax = request.MontoMax,
            PageSize = PaginationLimits.MaxPageSize,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        if (result.Value.ExceedsExportLimit())
        {
            return ExportExtensions.TooManyRowsResult();
        }

        var rows = result.Value.Items.Select(d => new object?[]
        {
            d.NumeroNotaCredito,
            d.CodigoVenta,
            d.FechaDevolucion.ToString(request.DateFormat),
            d.ClienteNombre,
            d.EmpleadoAutorizaNombre,
            d.SucursalNombre,
            d.MotivoSunatCatalogo,
            d.MontoNotaCredito,
            d.MontoReembolsado,
            d.MetodoReembolso,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "devoluciones.xlsx");
    }
}

public record DevolucionExportRequest(
    [ExportHeaders] string[] Headers,
    [ExportDateFormat] string DateFormat,
    Guid? EmpleadoAutorizaId,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    MetodoPago? MetodoReembolso,
    DestinoDevolucion? Destino,
    string? MotivoSunatCatalogo,
    string? NumeroNotaCredito,
    decimal? MontoMin,
    decimal? MontoMax);

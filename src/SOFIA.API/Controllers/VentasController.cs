using SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;
using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Application.Ventas.Commands.CompletarVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Queries.GetVentaById;
using SOFIA.Application.Ventas.Queries.GetVentas;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VentasController(ISender sender) : ControllerBase
{
    [HasPermission("Ventas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetVentasQuery query)
    {
        var result = await sender.Send(query);

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetVentaByIdQuery(id));

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVentaCommand command)
    {
        var result = await sender.Send(command);

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Actualizar")]
    [HttpPut("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, [FromBody] string motivo)
    {
        var result = await sender.Send(new AnularVentaCommand(id, motivo));

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Actualizar")]
    [HttpPut("{id:guid}/completar")]
    public async Task<IActionResult> Completar(Guid id, [FromBody] CompletarVentaBody body)
    {
        var result = await sender.Send(new CompletarVentaCommand(id, body.Pagos, body.Detalles, body.ClienteId, body.AseguradoraId, body.MontoCubiertoSeguro));

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Actualizar")]
    [HttpPut("{id:guid}/detalles")]
    public async Task<IActionResult> ActualizarDetalles(Guid id, [FromBody] ActualizarVentaPendienteBody body)
    {
        var result = await sender.Send(new ActualizarVentaPendienteCommand(id, body.Detalles, body.ClienteId));

        return result.ToActionResult();
    }

    [HasPermission("Ventas", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] VentaExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVentasQuery
        {
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Estado = request.Estado,
            EmpleadoId = request.EmpleadoId,
            ClienteId = request.ClienteId,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(v => new object?[]
        {
            v.CodigoVenta,
            v.FechaHora.ToString(request.DateFormat),
            v.ClienteNombre,
            v.EmpleadoNombre,
            v.Total,
            v.Estado,
            v.ItemsCount,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ventas.xlsx");
    }
}

public record VentaExportRequest(
    string[] Headers,
    string DateFormat,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    EstadoVenta? Estado,
    Guid? EmpleadoId,
    Guid? ClienteId);

public record CompletarVentaBody(
    List<CreateVentaPagoDto> Pagos,
    List<CreateVentaDetailDto>? Detalles,
    Guid? ClienteId,
    Guid? AseguradoraId,
    decimal? MontoCubiertoSeguro);

public record ActualizarVentaPendienteBody(
    List<CreateVentaDetailDto> Detalles,
    Guid? ClienteId);

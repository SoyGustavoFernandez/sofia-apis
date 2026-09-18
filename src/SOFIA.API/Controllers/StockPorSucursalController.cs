using SOFIA.Application.Inventarios.Commands.AdjustStock;
using SOFIA.Application.Inventarios.Queries.GetStockPorSucursal;
using SOFIA.Application.Inventarios.Queries.GetStockPorSucursalById;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class StockPorSucursalController(ISender sender) : ControllerBase
{
    [HasPermission("Inventarios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetStockPorSucursalQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetStockPorSucursalByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Inventarios", "Actualizar")]
    [HttpPut("{id:guid}/ajustar")]
    public async Task<IActionResult> Ajustar(Guid id, [FromBody] AdjustStockRequest request)
    {
        var result = await sender.Send(new AdjustStockCommand(id, request.NuevaCantidad));
        return result.ToActionResult();
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] StockPorSucursalExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStockPorSucursalQuery
        {
            SucursalNombre = request.SucursalNombre,
            ProductoNombre = request.ProductoNombre,
            NumeroLote = request.NumeroLote,
            CaducidadDesde = request.CaducidadDesde,
            CaducidadHasta = request.CaducidadHasta,
            CantidadMin = request.CantidadMin,
            CantidadMax = request.CantidadMax,
            SoloConStock = request.SoloConStock,
            PageSize = int.MaxValue,
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(s => new object?[]
        {
            s.SucursalNombre,
            s.ProductoNombre,
            s.NumeroLote,
            s.FechaCaducidad.ToString(request.DateFormat),
            s.CantidadFisica,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "stock-por-sucursal.xlsx");
    }
}

public record AdjustStockRequest(decimal NuevaCantidad);

public record StockPorSucursalExportRequest(
    string[] Headers,
    string DateFormat,
    string? SucursalNombre,
    string? ProductoNombre,
    string? NumeroLote,
    DateTimeOffset? CaducidadDesde,
    DateTimeOffset? CaducidadHasta,
    decimal? CantidadMin,
    decimal? CantidadMax,
    bool SoloConStock = true);

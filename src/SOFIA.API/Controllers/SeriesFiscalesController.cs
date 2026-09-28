using SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;
using SOFIA.Application.SeriesFiscales.Commands.DeleteSerieFiscal;
using SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;
using SOFIA.Application.SeriesFiscales.Queries.GetSerieFiscalById;
using SOFIA.Application.SeriesFiscales.Queries.GetSeriesFiscales;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SeriesFiscalesController(ISender sender) : ControllerBase
{
    [HasPermission("SeriesFiscales", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetSeriesFiscalesQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("SeriesFiscales", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetSerieFiscalByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("SeriesFiscales", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSerieFiscalCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("SeriesFiscales", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSerieFiscalCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToActionResult();
    }

    [HasPermission("SeriesFiscales", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteSerieFiscalCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("SeriesFiscales", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] SerieFiscalExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeriesFiscalesQuery
        {
            SucursalId = request.SucursalId,
            TipoComprobante = request.TipoComprobante,
            EstadoSerie = request.EstadoSerie,
            PrefijoSerie = request.PrefijoSerie,
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

        var rows = result.Value.Items.Select(s => new object?[]
        {
            s.PrefijoSerie,
            request.TipoLabels.GetValueOrDefault(s.TipoComprobante.ToString(), s.TipoComprobante.ToString()),
            s.SucursalNombre,
            s.CorrelativoActual,
            request.EstadoLabels.GetValueOrDefault(s.EstadoSerie, s.EstadoSerie),
            s.CreatedAt.ToString(request.DateFormat),
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "series-fiscales.xlsx");
    }
}

public record SerieFiscalExportRequest(
    string[] Headers,
    string DateFormat,
    Dictionary<string, string> TipoLabels,
    Dictionary<string, string> EstadoLabels,
    Guid? SucursalId,
    TipoComprobante? TipoComprobante,
    string? EstadoSerie,
    string? PrefijoSerie);

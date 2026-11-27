using SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Commands.UpdateServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Queries.GetServicioCatalogoById;
using SOFIA.Application.CatalogoServicios.Queries.GetServiciosCatalogo;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CatalogoServiciosController(ISender sender) : ControllerBase
{
    [HasPermission("Servicios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetServiciosCatalogoQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetServicioCatalogoByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServicioCatalogoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Servicios", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServicioCatalogoCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteServicioCatalogoCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] ServicioCatalogoExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetServiciosCatalogoQuery
        {
            Nombre = request.Nombre,
            PrecioMin = request.PrecioMin,
            PrecioMax = request.PrecioMax,
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
            s.Nombre,
            s.Precio,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "servicios.xlsx");
    }
}

public record ServicioCatalogoExportRequest(
    [ExportHeaders] string[] Headers,
    string? Nombre,
    decimal? PrecioMin,
    decimal? PrecioMax);

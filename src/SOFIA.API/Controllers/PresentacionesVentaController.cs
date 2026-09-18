using SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;
using SOFIA.Application.PresentacionesVenta.Commands.DeletePresentacionVenta;
using SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;
using SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionesVenta;
using SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionVentaById;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PresentacionesVentaController(ISender sender) : ControllerBase
{
    [HasPermission("PresentacionesVenta", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetPresentacionesVentaQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("PresentacionesVenta", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetPresentacionVentaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("PresentacionesVenta", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePresentacionVentaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("PresentacionesVenta", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePresentacionVentaCommand command)
    {
        command = command with { Id = id };

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("PresentacionesVenta", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeletePresentacionVentaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("PresentacionesVenta", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] PresentacionVentaExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPresentacionesVentaQuery
        {
            ProductoNombre = request.ProductoNombre,
            Descripcion = request.Descripcion,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(p => new object?[]
        {
            p.ProductoNombre,
            p.Descripcion,
            p.CantidadUnidadesBase,
            p.PrecioVenta,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "presentaciones-venta.xlsx");
    }
}

public record PresentacionVentaExportRequest(
    string[] Headers,
    string? ProductoNombre,
    string? Descripcion);

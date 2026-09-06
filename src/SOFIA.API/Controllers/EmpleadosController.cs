using SOFIA.Application.Empleados.Commands.CreateEmpleado;
using SOFIA.Application.Empleados.Commands.DeleteEmpleado;
using SOFIA.Application.Empleados.Commands.UpdateEmpleado;
using SOFIA.Application.Empleados.Queries.GetById;
using SOFIA.Application.Empleados.Queries.GetEmpleadosWithPagination;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmpleadosController(ISender sender) : ControllerBase
{
    [HasPermission("Empleados", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetEmpleadosWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empleados", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetEmpleadoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empleados", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmpleadoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empleados", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmpleadoCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empleados", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteEmpleadoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empleados", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] EmpleadoExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmpleadosWithPaginationQuery
        {
            Nombres = request.Nombres,
            ApellidoPaterno = request.ApellidoPaterno,
            ApellidoMaterno = request.ApellidoMaterno,
            Licencia = request.Licencia,
            SucursalNombre = request.SucursalNombre,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(e => new object?[]
        {
            e.Nombres,
            e.Apellido_Paterno,
            e.Apellido_Materno,
            e.Licencia_Prof,
            e.SucursalNombre,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "empleados.xlsx");
    }
}

public record EmpleadoExportRequest(
    string[] Headers,
    string? Nombres,
    string? ApellidoPaterno,
    string? ApellidoMaterno,
    string? Licencia,
    string? SucursalNombre);

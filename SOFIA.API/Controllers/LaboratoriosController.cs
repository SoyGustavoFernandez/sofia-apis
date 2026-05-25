using SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;
using SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;
using SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorioById;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorios;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LaboratoriosController(ISender sender) : ControllerBase
{
    [HasPermission("Laboratorios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetLaboratoriosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetLaboratorioByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLaboratorioCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLaboratorioCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteLaboratorioCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

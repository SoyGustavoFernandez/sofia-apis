using SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredienteActivoById;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredientesActivos;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientesActivosController(ISender sender) : ControllerBase
{
    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetIngredientesActivosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetIngredienteActivoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngredienteActivoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIngredienteActivoCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteIngredienteActivoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

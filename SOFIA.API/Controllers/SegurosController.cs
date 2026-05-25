using SOFIA.Application.Seguros.Commands.DeleteAseguradora;
using SOFIA.Application.Seguros.Commands.UpdateAseguradora;
using SOFIA.Application.Seguros.Queries.GetAseguradoraById;
using SOFIA.Application.Seguros.Queries.GetAseguradoras;
using SOFIA.Application.Seguros.Commands.CreateAseguradora;

namespace SOFIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SegurosController(ISender sender) : ControllerBase
{

    [HasPermission("Seguros", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateAseguradora([FromBody] CreateAseguradoraCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Seguros", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAseguradoras([FromQuery] GetAseguradorasQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HttpGet("{id}")]
    [HasPermission("Seguros", "Leer")]
    public async Task<IActionResult> GetAseguradoraById(Guid id)
    {
        var result = await sender.Send(new GetAseguradoraByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("{id}")]
    [HasPermission("Seguros", "Actualizar")]
    public async Task<IActionResult> UpdateAseguradora(Guid id, [FromBody] UpdateAseguradoraCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("El ID de la ruta no coincide con el comando.");
        }

        var result = await sender.Send(command);
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id}")]
    [HasPermission("Seguros", "Eliminar")]
    public async Task<IActionResult> DeleteAseguradora(Guid id)
    {
        var result = await sender.Send(new DeleteAseguradoraCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

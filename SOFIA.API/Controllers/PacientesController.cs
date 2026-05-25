using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Application.Pacientes.Commands.UpdatePaciente;
using SOFIA.Application.Pacientes.Queries.GetPacienteById;
using SOFIA.Application.Pacientes.Queries.GetPacientes;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;

namespace SOFIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PacientesController(ISender sender) : ControllerBase
{

    [HasPermission("Pacientes", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreatePaciente([FromBody] CreatePacienteCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Pacientes", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPacientes([FromQuery] GetPacientesQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HttpGet("{id}")]
    [HasPermission("Pacientes", "Leer")]
    public async Task<IActionResult> GetPacienteById(Guid id)
    {
        var result = await sender.Send(new GetPacienteByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("{id}")]
    [HasPermission("Pacientes", "Actualizar")]
    public async Task<IActionResult> UpdatePaciente(Guid id, [FromBody] UpdatePacienteCommand command)
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
    [HasPermission("Pacientes", "Eliminar")]
    public async Task<IActionResult> DeletePaciente(Guid id)
    {
        var result = await sender.Send(new DeletePacienteCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

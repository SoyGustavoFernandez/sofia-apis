using SOFIA.Application.Citas.Commands.CerrarCita;
using SOFIA.Application.Citas.Commands.ProgramarCita;
using SOFIA.Application.Citas.Commands.ReprogramarCita;
using SOFIA.Application.Citas.Queries.GetCitaById;
using SOFIA.Application.Citas.Queries.GetCitas;
using SOFIA.Application.Citas.Queries.GetComprasPaciente;
using SOFIA.Domain.Enums;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CitasController(ISender sender) : ControllerBase
{
    [HasPermission("Servicios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetCitasQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetCitaByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Leer")]
    [HttpGet("compras-paciente/{pacienteId:guid}")]
    public async Task<IActionResult> ComprasPaciente(Guid pacienteId)
    {
        var result = await sender.Send(new GetComprasPacienteQuery(pacienteId));
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Programar([FromBody] ProgramarCitaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Servicios", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Reprogramar(Guid id, [FromBody] ReprogramarCitaCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToActionResult();
    }

    [HasPermission("Servicios", "Actualizar")]
    [HttpPut("{id:guid}/estado/{estado}")]
    public async Task<IActionResult> Cerrar(Guid id, EstadoCita estado)
    {
        var result = await sender.Send(new CerrarCitaCommand(id, estado));
        return result.ToActionResult();
    }
}

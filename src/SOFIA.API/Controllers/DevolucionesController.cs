using SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DevolucionesController(ISender sender) : ControllerBase
{

    [HasPermission("Devoluciones", "ProcesarDevolucion")]
    [HttpPost]
    public async Task<IActionResult> ProcesarDevolucion([FromBody] ProcesarDevolucionCommand request)
    {
        var result = await sender.Send(request);
        return result.ToActionResult();
    }

    [HasPermission("Devoluciones", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetDevoluciones([FromQuery] Guid? empleadoAutorizaId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Devoluciones.Queries.GetDevoluciones.GetDevolucionesQuery(empleadoAutorizaId, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult();
    }

    [HttpGet("{id}")]
    [HasPermission("Devoluciones", "Leer")]
    public async Task<IActionResult> GetDevolucionById(Guid id)
    {
        var result = await sender.Send(new Application.Devoluciones.Queries.GetDevolucionById.GetDevolucionByIdQuery(id));
        return !result.IsSuccess ? result.ToProblemResult() : Ok(result);
    }
}

using SOFIA.Application.POS.Commands.AperturarCaja;
using SOFIA.Application.POS.Commands.CerrarCaja;

namespace SOFIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POSController(ISender sender) : ControllerBase
{

    [HasPermission("POS", "AperturarCaja")]
    [HttpPost("apertura")]
    public async Task<IActionResult> AperturarCaja([FromBody] AperturarCajaCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("POS", "CerrarCaja")]
    [HttpPost("cierre")]
    public async Task<IActionResult> CerrarCaja([FromBody] CerrarCajaCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HasPermission("POS", "Leer")]
    [HttpGet("sesiones")]
    public async Task<IActionResult> GetSesiones([FromQuery] Guid? sucursalId, [FromQuery] Domain.Enums.EstadoSesion? estadoSesion, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.POS.Queries.GetSesiones.GetSesionesQuery(sucursalId, estadoSesion, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HttpGet("{id}")]
    [HasPermission("POS", "Leer")]
    public async Task<IActionResult> GetSesionCajaById(Guid id)
    {
        var result = await sender.Send(new Application.POS.Queries.GetSesionById.GetSesionByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }
}

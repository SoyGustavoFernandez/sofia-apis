using SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;
using SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DIGEMIDController(ISender sender) : ControllerBase
{

    [HasPermission("DIGEMID", "AislarLoteCuarentena")]
    [HttpPost("cuarentena")]
    public async Task<IActionResult> AislarLoteCuarentena([FromBody] AislarLoteCuarentenaCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("DIGEMID", "GenerarActaDestruccion")]
    [HttpPost("acta-destruccion")]
    public async Task<IActionResult> GenerarActaDestruccion([FromBody] GenerarActaDestruccionCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("cuarentena")]
    public async Task<IActionResult> GetCuarentena([FromQuery] Guid? sucursalId, [FromQuery] string? estadoResolucion, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.DIGEMID.Queries.GetCuarentena.GetCuarentenaQuery(sucursalId, estadoResolucion, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("actas")]
    public async Task<IActionResult> GetActas([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.DIGEMID.Queries.GetActas.GetActasQuery(fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

}

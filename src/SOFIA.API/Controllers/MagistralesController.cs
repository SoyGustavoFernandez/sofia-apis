using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;
using SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;

namespace SOFIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MagistralesController(ISender sender) : ControllerBase
{

    [HasPermission("Magistrales", "IniciarOrdenMagistral")]
    [HttpPost("iniciar")]
    public async Task<IActionResult> IniciarOrdenMagistral([FromBody] IniciarOrdenMagistralCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Magistrales", "CompletarOrdenMagistral")]
    [HttpPost("completar")]
    public async Task<IActionResult> CompletarOrdenMagistral([FromBody] CompletarOrdenMagistralCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HasPermission("Magistrales", "Leer")]
    [HttpGet("ordenes")]
    public async Task<IActionResult> GetOrdenes([FromQuery] Guid? sucursalId, [FromQuery] string? estadoProduccion, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Magistrales.Queries.GetOrdenes.GetOrdenesQuery(sucursalId, estadoProduccion, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HttpGet("{id}")]
    [HasPermission("Magistrales", "Leer")]
    public async Task<IActionResult> GetOrdenMagistralById(Guid id)
    {
        var result = await sender.Send(new Application.Magistrales.Queries.GetOrdenById.GetOrdenByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }
}

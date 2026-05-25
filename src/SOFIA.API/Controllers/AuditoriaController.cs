using SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuditoriaController(ISender sender) : ControllerBase
{

    [HasPermission("Auditoria", "AnonimizarDatos")]
    [HttpPost("anonimizar")]
    public async Task<IActionResult> AnonimizarDatos([FromBody] AnonimizarDatosCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

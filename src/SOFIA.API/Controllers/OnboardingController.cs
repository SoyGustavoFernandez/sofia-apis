using SOFIA.Application.Onboarding.Commands.CrearUnidadesBasicas;
using SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;
using SOFIA.Application.Onboarding.Queries.GetPrimerosPasos;

namespace SOFIA.API.Controllers;

// First steps of a new company; the handlers restrict them to the company administrator
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class OnboardingController(ISender sender) : ControllerBase
{
    [HttpGet("primeros-pasos")]
    public async Task<IActionResult> GetPrimerosPasos()
    {
        var result = await sender.Send(new GetPrimerosPasosQuery());

        return result.ToActionResult();
    }

    [HttpPost("primer-producto")]
    public async Task<IActionResult> RegistrarPrimerProducto([FromBody] RegistrarPrimerProductoCommand command)
    {
        var result = await sender.Send(command);

        return result.ToActionResult();
    }

    [HttpPost("unidades-basicas")]
    public async Task<IActionResult> CrearUnidadesBasicas()
    {
        var result = await sender.Send(new CrearUnidadesBasicasCommand());

        return result.ToActionResult();
    }
}

using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController(ISender sender) : ControllerBase
{
    [HasPermission("Ventas", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVentaCommand command)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Ventas", "Actualizar")]
    [HttpPut("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, [FromBody] string motivo)
    {
        var result = await sender.Send(new AnularVentaCommand(id, motivo));

        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

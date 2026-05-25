using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Queries.GetVentaById;
using SOFIA.Application.Ventas.Queries.GetVentas;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VentasController(ISender sender) : ControllerBase
{
    [HasPermission("Ventas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetVentasQuery query)
    {
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Ventas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetVentaByIdQuery(id));

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

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

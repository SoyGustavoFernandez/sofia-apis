using SOFIA.Application.Delivery.Commands.DeleteDespachoDelivery;
using SOFIA.Application.Delivery.Commands.UpdateDespachoDelivery;
using SOFIA.Application.Delivery.Queries.GetDespachoDeliveryById;
using SOFIA.Application.Delivery.Commands.UpdateDeliveryEstado;
using SOFIA.Application.Delivery.Commands.ProgramarDelivery;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeliveryController(ISender sender) : ControllerBase
{

    [HasPermission("Delivery", "ProgramarDelivery")]
    [HttpPost]
    public async Task<IActionResult> ProgramarDelivery([FromBody] ProgramarDeliveryCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Delivery", "Actualizar")]
    [HttpPut("{id}/estado")]
    public async Task<IActionResult> UpdateDeliveryEstado([FromBody] UpdateDeliveryEstadoCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("Delivery", "Leer")]
    [HttpGet("despachos")]
    public async Task<IActionResult> GetDespachos([FromQuery] Domain.Enums.EstadoDespacho? estadoDespacho, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Delivery.Queries.GetDespachos.GetDespachosQuery(estadoDespacho, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    [HasPermission("Delivery", "Leer")]
    public async Task<IActionResult> GetDespachoDeliveryById(Guid id)
    {
        var result = await sender.Send(new GetDespachoDeliveryByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("{id}")]
    [HasPermission("Delivery", "Actualizar")]
    public async Task<IActionResult> UpdateDespachoDelivery(Guid id, [FromBody] UpdateDespachoDeliveryCommand command)
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
    [HasPermission("Delivery", "Eliminar")]
    public async Task<IActionResult> DeleteDespachoDelivery(Guid id)
    {
        var result = await sender.Send(new DeleteDespachoDeliveryCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

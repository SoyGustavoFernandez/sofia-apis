using SOFIA.Application.Servicios.Commands.DeleteInmunizacion;
using SOFIA.Application.Servicios.Commands.UpdateInmunizacion;
using SOFIA.Application.Servicios.Queries.GetInmunizacionById;
using SOFIA.Application.Servicios.Commands.DeleteServicio;
using SOFIA.Application.Servicios.Commands.UpdateServicio;
using SOFIA.Application.Servicios.Queries.GetServicioById;
using SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;
using SOFIA.Application.Servicios.Commands.AgendarServicio;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ServiciosController(ISender sender) : ControllerBase
{

    [HasPermission("Servicios", "AgendarServicio")]
    [HttpPost("agenda")]
    public async Task<IActionResult> AgendarServicio([FromBody] AgendarServicioCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Servicios", "RegistrarInmunizacion")]
    [HttpPost("inmunizacion")]
    public async Task<IActionResult> RegistrarInmunizacion([FromBody] RegistrarInmunizacionCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("Servicios", "Leer")]
    [HttpGet("agenda")]
    public async Task<IActionResult> GetServicios([FromQuery] string? estadoCita, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Servicios.Queries.GetServicios.GetServiciosQuery(estadoCita, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("Servicios", "Leer")]
    [HttpGet("inmunizaciones")]
    public async Task<IActionResult> GetInmunizaciones([FromQuery] Guid? clienteId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Servicios.Queries.GetInmunizaciones.GetInmunizacionesQuery(clienteId, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("servicio/{id}")]
    [HasPermission("Servicios", "Leer")]
    public async Task<IActionResult> GetServicioById(Guid id)
    {
        var result = await sender.Send(new GetServicioByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("servicio/{id}")]
    [HasPermission("Servicios", "Actualizar")]
    public async Task<IActionResult> UpdateServicio(Guid id, [FromBody] UpdateServicioCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID de la ruta no coincide con el comando.");
        }

        var result = await sender.Send(command);
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("servicio/{id}")]
    [HasPermission("Servicios", "Eliminar")]
    public async Task<IActionResult> DeleteServicio(Guid id)
    {
        var result = await sender.Send(new DeleteServicioCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpGet("inmunizacion/{id}")]
    [HasPermission("Servicios", "Leer")]
    public async Task<IActionResult> GetInmunizacionById(Guid id)
    {
        var result = await sender.Send(new GetInmunizacionByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("inmunizacion/{id}")]
    [HasPermission("Servicios", "Actualizar")]
    public async Task<IActionResult> UpdateInmunizacion(Guid id, [FromBody] UpdateInmunizacionCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID de la ruta no coincide con el comando.");
        }

        var result = await sender.Send(command);
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("inmunizacion/{id}")]
    [HasPermission("Servicios", "Eliminar")]
    public async Task<IActionResult> DeleteInmunizacion(Guid id)
    {
        var result = await sender.Send(new DeleteInmunizacionCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

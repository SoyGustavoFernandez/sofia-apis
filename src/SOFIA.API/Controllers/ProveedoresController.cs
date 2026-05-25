using SOFIA.Application.Proveedores.Commands.DeleteProveedor;
using SOFIA.Application.Proveedores.Commands.UpdateProveedor;
using SOFIA.Application.Proveedores.Queries.GetProveedorById;
using SOFIA.Application.Proveedores.Commands.CreateProveedor;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;
using SOFIA.Application.Proveedores.Queries.GetProveedores;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProveedoresController(ISender sender) : ControllerBase
{

    [HasPermission("Proveedores", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateProveedor([FromBody] CreateProveedorCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Proveedores", "RegistrarPrecioProveedor")]
    [HttpPost("precios")]
    public async Task<IActionResult> RegistrarPrecioProveedor([FromBody] RegistrarPrecioProveedorCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Proveedores", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetProveedores([FromQuery] GetProveedoresQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }


    [HttpGet("{id}")]
    [HasPermission("Proveedores", "Leer")]
    public async Task<IActionResult> GetProveedorById(Guid id)
    {
        var result = await sender.Send(new GetProveedorByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("{id}")]
    [HasPermission("Proveedores", "Actualizar")]
    public async Task<IActionResult> UpdateProveedor(Guid id, [FromBody] UpdateProveedorCommand command)
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
    [HasPermission("Proveedores", "Eliminar")]
    public async Task<IActionResult> DeleteProveedor(Guid id)
    {
        var result = await sender.Send(new DeleteProveedorCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

using SOFIA.Application.Security.Commands.Cuentas.AssignSucursal;
using SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;
using SOFIA.Application.Security.Commands.Cuentas.RemoveSucursal;
using SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.Application.Security.Commands.Roles.AssignRol;
using SOFIA.Application.Security.Commands.Roles.RemoveRol;
using SOFIA.Application.Security.Queries.Cuentas.GetCuentaById;
using SOFIA.Application.Security.Queries.Cuentas.GetCuentas;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CuentasController(ISender sender) : ControllerBase
{
    [HasPermission("Seguridad", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetCuentas(
        [FromQuery] string? nombreUsuario,
        [FromQuery] string? nombreEmpleado,
        [FromQuery] bool? cuentaActiva,
        [FromQuery] bool? bloqueado,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await sender.Send(new GetCuentasQuery
        {
            NombreUsuario = nombreUsuario,
            NombreEmpleado = nombreEmpleado,
            CuentaActiva = cuentaActiva,
            Bloqueado = bloqueado,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCuentaById(Guid id)
    {
        var result = await sender.Send(new GetCuentaByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateCuenta([FromBody] RegisterAccountCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCuentaById), new { id = result.Value }, new { id = result.Value })
            : result.ToProblemResult();
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCuenta(Guid id, [FromBody] UpdateCuentaRequest request)
    {
        var result = await sender.Send(new UpdateCuentaCommand(
            id,
            request.CuentaActiva,
            request.ForzarCambioClave,
            request.ResetearIntentos));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCuenta(Guid id)
    {
        var result = await sender.Send(new DeleteCuentaCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpPost("{id:guid}/roles/{rolId:guid}")]
    public async Task<IActionResult> AssignRol(Guid id, Guid rolId)
    {
        var result = await sender.Send(new AssignRolToUserCommand(id, rolId));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpDelete("{id:guid}/roles/{rolId:guid}")]
    public async Task<IActionResult> RemoveRol(Guid id, Guid rolId)
    {
        var result = await sender.Send(new RemoveRolFromUserCommand(id, rolId));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpPost("{id:guid}/sucursales/{sucursalId:guid}")]
    public async Task<IActionResult> AssignSucursal(Guid id, Guid sucursalId)
    {
        var result = await sender.Send(new AssignSucursalToCuentaCommand(id, sucursalId));
        return result.ToActionResult();
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpDelete("{id:guid}/sucursales/{sucursalId:guid}")]
    public async Task<IActionResult> RemoveSucursal(Guid id, Guid sucursalId)
    {
        var result = await sender.Send(new RemoveSucursalFromCuentaCommand(id, sucursalId));
        return result.ToActionResult();
    }
}

public record UpdateCuentaRequest(bool? CuentaActiva, bool? ForzarCambioClave, bool ResetearIntentos);

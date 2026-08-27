using SOFIA.Application.Empresas.Commands.DeleteEmpresa;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Application.Empresas.Commands.UpdateEmpresa;
using SOFIA.Application.Empresas.Queries.GetEmpresaById;
using SOFIA.Application.Empresas.Queries.GetEmpresas;
using SOFIA.Domain.Entities;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmpresasController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Registro público: crea empresa, sede principal, admin y devuelve JWT para ingreso inmediato.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEmpresaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? StatusCode(201, new { token = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EstadoEmpresa? estado)
    {
        var result = await sender.Send(new GetEmpresasQuery(estado));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetEmpresaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmpresaCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteEmpresaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

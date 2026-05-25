using SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiaUoMById;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiasUoM;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class JerarquiasUoMController(ISender sender) : ControllerBase
{
    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetJerarquiasUoMQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetJerarquiaUoMByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJerarquiaUoMCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJerarquiaUoMCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteJerarquiaUoMCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

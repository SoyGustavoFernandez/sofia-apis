using MediatR;
using SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadMedidaById;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnidadesMedidaController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetUnidadesMedidaQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetUnidadMedidaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnidadMedidaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnidadMedidaCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteUnidadMedidaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

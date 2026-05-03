using MediatR;
using SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiaUoMById;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiasUoM;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JerarquiasUoMController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetJerarquiasUoMQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetJerarquiaUoMByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJerarquiaUoMCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJerarquiaUoMCommand command)
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
        var result = await sender.Send(new DeleteJerarquiaUoMCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

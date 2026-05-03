using MediatR;
using SOFIA.Application.Sucursales.Commands.CreateSucursal;
using SOFIA.Application.Sucursales.Commands.DeleteSucursal;
using SOFIA.Application.Sucursales.Commands.UpdateSucursal;
using SOFIA.Application.Sucursales.Queries.GetById;
using SOFIA.Application.Sucursales.Queries.GetSucursalesWithPagination;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SucursalesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetSucursalesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetSucursalByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSucursalCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSucursalCommand command)
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
        var result = await sender.Send(new DeleteSucursalCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

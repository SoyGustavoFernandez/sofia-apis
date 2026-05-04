using MediatR;
using SOFIA.Application.FormulacionesClinicas.Commands.Create;
using SOFIA.Application.FormulacionesClinicas.Commands.Delete;
using SOFIA.Application.FormulacionesClinicas.Commands.Update;
using SOFIA.Application.FormulacionesClinicas.Queries.GetById;
using SOFIA.Application.FormulacionesClinicas.Queries.GetByMedicamento;
using SOFIA.Application.FormulacionesClinicas.Queries.GetFormulacionesClinicasWithPagination;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormulacionesClinicasController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetFormulacionesClinicasWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
    [HttpGet("medicamento/{productoId:guid}")]
    public async Task<IActionResult> GetByMedicamento(Guid productoId)
    {
        var result = await sender.Send(new GetFormulacionesByMedicamentoQuery(productoId));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetFormulacionByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormulacionClinicaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormulacionClinicaCommand command)
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
        var result = await sender.Send(new DeleteFormulacionClinicaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

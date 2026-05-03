using MediatR;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;
using SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;
using SOFIA.Application.Medicamentos.Queries.GetMedicamentoById;
using SOFIA.Application.Medicamentos.Queries.GetMedicamentos;
using SOFIA.Domain.Entities;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicamentosController(ISender sender) : ControllerBase
{
    [HttpGet("condiciones-venta")]
    public IActionResult GetCondicionesVenta() => Ok(Medicamento.CondicionesValidas);

    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetMedicamentosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetMedicamentoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicamentoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicamentoCommand command)
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
        var result = await sender.Send(new DeleteMedicamentoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

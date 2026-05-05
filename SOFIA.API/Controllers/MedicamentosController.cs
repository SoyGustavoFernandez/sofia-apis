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
    [HasPermission("Medicamentos", "Leer")]
    [HttpGet("condiciones-venta")]
    public IActionResult GetCondicionesVenta() => Ok(Medicamento.CondicionesValidas);

    [HasPermission("Medicamentos", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetMedicamentosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Medicamentos", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetMedicamentoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Medicamentos", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicamentoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Medicamentos", "Actualizar")]
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

    [HasPermission("Medicamentos", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteMedicamentoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

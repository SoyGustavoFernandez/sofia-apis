using SOFIA.Application.FormulacionesClinicas.Commands.Create;
using SOFIA.Application.FormulacionesClinicas.Commands.Delete;
using SOFIA.Application.FormulacionesClinicas.Commands.Update;
using SOFIA.Application.FormulacionesClinicas.Queries.GetById;
using SOFIA.Application.FormulacionesClinicas.Queries.GetByMedicamento;
using SOFIA.Application.FormulacionesClinicas.Queries.GetFormulacionesClinicasWithPagination;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class FormulacionesClinicasController(ISender sender) : ControllerBase
{
    [HasPermission("FormulacionesClinicas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetFormulacionesClinicasWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }
    [HasPermission("FormulacionesClinicas", "Leer")]
    [HttpGet("medicamento/{productoId:guid}")]
    public async Task<IActionResult> GetByMedicamento(Guid productoId)
    {
        var result = await sender.Send(new GetFormulacionesByMedicamentoQuery(productoId));
        return result.ToActionResult();
    }

    [HasPermission("FormulacionesClinicas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetFormulacionByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("FormulacionesClinicas", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormulacionClinicaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("FormulacionesClinicas", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormulacionClinicaCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HasPermission("FormulacionesClinicas", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteFormulacionClinicaCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("FormulacionesClinicas", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] FormulacionClinicaExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFormulacionesClinicasWithPaginationQuery
        {
            ProductoNombre = request.ProductoNombre,
            IngredienteNombre = request.IngredienteNombre,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(f => new object?[]
        {
            f.ProductoNombre,
            f.IngredienteNombre,
            f.ConcentracionDosis,
            f.UnidadMedidaNombre,
            f.CodigoTeOrange,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "formulaciones-clinicas.xlsx");
    }
}

public record FormulacionClinicaExportRequest(string[] Headers, string? ProductoNombre, string? IngredienteNombre);

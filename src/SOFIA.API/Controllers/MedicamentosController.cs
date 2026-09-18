using SOFIA.Application.Common.Excel;
using SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;
using SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;
using SOFIA.Application.Medicamentos.Queries.GetMedicamentoById;
using SOFIA.Application.Medicamentos.Queries.GetMedicamentos;
using SOFIA.Application.Medicamentos.Queries.PreviewImportMedicamentos;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MedicamentosController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    private static readonly string[] ImportColumns =
        ["CodigoNacional", "NombreComercial", "Laboratorio", "UnidadBase", "CondicionVenta"];

    [HasPermission("Medicamentos", "Leer")]
    [HttpGet("condiciones-venta")]
    public IActionResult GetCondicionesVenta() => Ok(Medicamento.CondicionesValidas);

    [HasPermission("Medicamentos", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetMedicamentosQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Medicamentos", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetMedicamentoByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Medicamentos", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicamentoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Medicamentos", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicamentoCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HasPermission("Medicamentos", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteMedicamentoCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Medicamentos", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] MedicamentoExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMedicamentosQuery
        {
            CodigoNacional = request.CodigoNacional,
            NombreComercial = request.NombreComercial,
            LaboratorioNombre = request.LaboratorioNombre,
            UnidadBaseNombre = request.UnidadBaseNombre,
            CondicionVenta = request.CondicionVenta.HasValue ? (CondicionVenta)request.CondicionVenta.Value : null,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(m => new object?[]
        {
            m.CodigoNacional,
            m.NombreComercial,
            m.LaboratorioNombre,
            m.UnidadBaseNombre,
            Medicamento.CondicionesValidas[(int)m.CondicionVenta],
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "medicamentos.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var bytes = ExcelTemplateGenerator.GenerateTemplate(ImportColumns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-medicamentos.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, ImportColumns);
        var result = await sender.Send(new PreviewImportMedicamentosQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<MedicamentoImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaMedicamentosCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : result.ToProblemResult();
    }
}

public record MedicamentoExportRequest(
    string[] Headers,
    string? CodigoNacional,
    string? NombreComercial,
    string? LaboratorioNombre,
    string? UnidadBaseNombre,
    int? CondicionVenta);

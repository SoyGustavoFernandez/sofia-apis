using SOFIA.Application.Common.Excel;
using SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;
using SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;
using SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;
using SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorioById;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorios;
using SOFIA.Application.Laboratorios.Queries.PreviewImportLaboratorios;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LaboratoriosController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Laboratorios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetLaboratoriosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetLaboratorioByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLaboratorioCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLaboratorioCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteLaboratorioCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Laboratorios", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] LaboratorioExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLaboratoriosQuery
        {
            NombreCompania = request.NombreCompania,
            CodigoIdentificador = request.CodigoIdentificador,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(l => new object?[]
        {
            l.NombreCompania,
            l.CodigoIdentificador,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "laboratorios.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "NombreCompania", "CodigoIdentificador" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-laboratorios.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "NombreCompania", "CodigoIdentificador" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportLaboratoriosQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<LaboratorioImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaLaboratoriosCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record LaboratorioExportRequest(string[] Headers, string? NombreCompania, string? CodigoIdentificador);

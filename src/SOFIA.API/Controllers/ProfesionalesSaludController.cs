using SOFIA.Application.Common.Excel;
using SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;
using SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;
using SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;
using SOFIA.Application.Profesionales.Commands.UpdateProfesionalSalud;
using SOFIA.Application.Profesionales.Queries.GetProfesionalSaludById;
using SOFIA.Application.Profesionales.Queries.GetProfesionalesSalud;
using SOFIA.Application.Profesionales.Queries.PreviewImportProfesionalesSalud;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profesionalessalud")]
public class ProfesionalesSaludController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("ProfesionalesSalud", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetProfesionalesSaludQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("ProfesionalesSalud", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetProfesionalSaludByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("ProfesionalesSalud", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProfesionalSaludCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("ProfesionalesSalud", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProfesionalSaludCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("ProfesionalesSalud", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteProfesionalSaludCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "NumeroRegistro", "NombrePrescriptor", "DireccionClinica" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-profesionalessalud.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "NumeroRegistro", "NombrePrescriptor", "DireccionClinica" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportProfesionalesSaludQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<ProfesionalSaludImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaProfesionalesSaludCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("ProfesionalesSalud", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] ProfesionalSaludExportRequest request)
    {
        var result = await sender.Send(new GetProfesionalesSaludQuery
        {
            NumeroRegistro = request.NumeroRegistro,
            NombrePrescriptor = request.NombrePrescriptor,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(p => new object?[]
        {
            p.NumeroRegistro,
            p.NombrePrescriptor,
            p.DireccionClinica,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "profesionalessalud.xlsx");
    }
}

public record ProfesionalSaludExportRequest(
    string[] Headers,
    string? NumeroRegistro,
    string? NombrePrescriptor);

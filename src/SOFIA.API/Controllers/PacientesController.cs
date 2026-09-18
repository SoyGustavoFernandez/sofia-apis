using SOFIA.Application.Common.Excel;
using SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;
using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Application.Pacientes.Commands.UpdatePaciente;
using SOFIA.Application.Pacientes.Queries.GetPacienteById;
using SOFIA.Application.Pacientes.Queries.GetPacientes;
using SOFIA.Application.Pacientes.Queries.PreviewImportPacientes;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PacientesController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Pacientes", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreatePaciente([FromBody] CreatePacienteCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPacienteById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Pacientes", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPacientes([FromQuery] GetPacientesQuery request)
    {
        var result = await sender.Send(request);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    [HasPermission("Pacientes", "Leer")]
    public async Task<IActionResult> GetPacienteById(Guid id)
    {
        var result = await sender.Send(new GetPacienteByIdQuery(id));
        return result.ToActionResult();
    }

    [HttpPut("{id}")]
    [HasPermission("Pacientes", "Actualizar")]
    public async Task<IActionResult> UpdatePaciente(Guid id, [FromBody] UpdatePacienteCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [HasPermission("Pacientes", "Eliminar")]
    public async Task<IActionResult> DeletePaciente(Guid id)
    {
        var result = await sender.Send(new DeletePacienteCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Pacientes", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] PacienteExportRequest request)
    {
        var result = await sender.Send(new GetPacientesQuery
        {
            DocIdentidadGub = request.DocIdentidadGub,
            NombreApellidos = request.NombreApellidos,
            FechaNacimientoDesde = request.FechaNacimientoDesde,
            FechaNacimientoHasta = request.FechaNacimientoHasta,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(p => new object?[]
        {
            p.DocIdentidadGub,
            p.NombreApellidos,
            p.FechaNacimiento.ToString(request.DateFormat),
            p.ContactoPrimario,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "pacientes.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "DocIdentidadGub", "NombreApellidos", "FechaNacimiento", "ContactoPrimario" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-pacientes.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "DocIdentidadGub", "NombreApellidos", "FechaNacimiento", "ContactoPrimario" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportPacientesQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<PacienteImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaPacientesCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : result.ToProblemResult();
    }
}

public record PacienteExportRequest(
    string[] Headers,
    string DateFormat,
    string? DocIdentidadGub,
    string? NombreApellidos,
    DateOnly? FechaNacimientoDesde,
    DateOnly? FechaNacimientoHasta);

using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;
using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Application.Pacientes.Commands.UpdatePaciente;
using SOFIA.Application.Pacientes.Queries.GetPacienteById;
using SOFIA.Application.Pacientes.Queries.GetPacientes;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PacientesController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{

    [HasPermission("Pacientes", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreatePaciente([FromBody] CreatePacienteCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HasPermission("Pacientes", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPacientes([FromQuery] GetPacientesQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    [HasPermission("Pacientes", "Leer")]
    public async Task<IActionResult> GetPacienteById(Guid id)
    {
        var result = await sender.Send(new GetPacienteByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPut("{id}")]
    [HasPermission("Pacientes", "Actualizar")]
    public async Task<IActionResult> UpdatePaciente(Guid id, [FromBody] UpdatePacienteCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("El ID de la ruta no coincide con el comando.");
        }

        var result = await sender.Send(command);
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id}")]
    [HasPermission("Pacientes", "Eliminar")]
    public async Task<IActionResult> DeletePaciente(Guid id)
    {
        var result = await sender.Send(new DeletePacienteCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
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

        var existingDocs = await context.Pacientes
            .Select(p => p.DocIdentidadGub.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingDocs);

        var seenDocs = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var doc = row.Values.GetValueOrDefault("DocIdentidadGub");
            if (!string.IsNullOrWhiteSpace(doc))
            {
                if (!seenDocs.ContainsKey(doc))
                {
                    seenDocs[doc] = [];
                }

                seenDocs[doc].Add(row.RowNumber);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var doc = row.Values.GetValueOrDefault("DocIdentidadGub");
                var nombre = row.Values.GetValueOrDefault("NombreApellidos");
                var fechaStr = row.Values.GetValueOrDefault("FechaNacimiento");
                var contacto = row.Values.GetValueOrDefault("ContactoPrimario");

                if (string.IsNullOrWhiteSpace(doc))
                {
                    errors.Add(new ValidationError("required", "docIdentidadGub"));
                }
                else
                {
                    if (doc.Length > 50)
                    {
                        errors.Add(new ValidationError("max-length", "docIdentidadGub", new() { ["max"] = 50 }));
                    }

                    if (existingSet.Contains(doc.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "docIdentidadGub", new() { ["value"] = doc }));
                    }

                    if (seenDocs.TryGetValue(doc, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "docIdentidadGub", new() { ["value"] = doc }));
                    }
                }

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreApellidos"));
                }
                else if (nombre.Length > 200)
                {
                    errors.Add(new ValidationError("max-length", "nombreApellidos", new() { ["max"] = 200 }));
                }

                if (string.IsNullOrWhiteSpace(fechaStr))
                {
                    errors.Add(new ValidationError("required", "fechaNacimiento"));
                }
                else if (!DateOnly.TryParseExact(fechaStr, "dd/MM/yyyy", out var fecha))
                {
                    errors.Add(new ValidationError("invalid-date", "fechaNacimiento"));
                }
                else if (fecha > today)
                {
                    errors.Add(new ValidationError("future-date", "fechaNacimiento"));
                }

                if (!string.IsNullOrEmpty(contacto) && contacto.Length > 100)
                {
                    errors.Add(new ValidationError("max-length", "contactoPrimario", new() { ["max"] = 100 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record PacienteImportRow(string DocIdentidadGub, string NombreApellidos, string FechaNacimiento, string? ContactoPrimario);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<PacienteImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            if (!DateOnly.TryParseExact(row.FechaNacimiento, "dd/MM/yyyy", out var fecha))
            {
                continue;
            }

            var result = PacienteCliente.Create(row.DocIdentidadGub, row.NombreApellidos, fecha, row.ContactoPrimario);
            if (result.IsSuccess)
            {
                _ = context.Pacientes.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

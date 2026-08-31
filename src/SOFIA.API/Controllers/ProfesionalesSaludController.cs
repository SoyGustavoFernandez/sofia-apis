using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;
using SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;
using SOFIA.Application.Profesionales.Commands.UpdateProfesionalSalud;
using SOFIA.Application.Profesionales.Queries.GetProfesionalSaludById;
using SOFIA.Application.Profesionales.Queries.GetProfesionalesSalud;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profesionalessalud")]
public class ProfesionalesSaludController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
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

        var existingRegistros = await context.ProfesionalesSalud
            .Select(p => p.NumeroRegistro.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingRegistros);

        var seenRegistros = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var reg = row.Values.GetValueOrDefault("NumeroRegistro");
            if (!string.IsNullOrWhiteSpace(reg))
            {
                if (!seenRegistros.ContainsKey(reg))
                {
                    seenRegistros[reg] = [];
                }

                seenRegistros[reg].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var reg = row.Values.GetValueOrDefault("NumeroRegistro");
                var nombre = row.Values.GetValueOrDefault("NombrePrescriptor");
                var direccion = row.Values.GetValueOrDefault("DireccionClinica");

                if (string.IsNullOrWhiteSpace(reg))
                {
                    errors.Add(new ValidationError("required", "numeroRegistro"));
                }
                else
                {
                    if (reg.Length > 50)
                    {
                        errors.Add(new ValidationError("max-length", "numeroRegistro", new() { ["max"] = 50 }));
                    }

                    if (existingSet.Contains(reg.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "numeroRegistro", new() { ["value"] = reg }));
                    }

                    if (seenRegistros.TryGetValue(reg, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "numeroRegistro", new() { ["value"] = reg }));
                    }
                }

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombrePrescriptor"));
                }
                else if (nombre.Length > 150)
                {
                    errors.Add(new ValidationError("max-length", "nombrePrescriptor", new() { ["max"] = 150 }));
                }

                if (!string.IsNullOrEmpty(direccion) && direccion.Length > 255)
                {
                    errors.Add(new ValidationError("max-length", "direccionClinica", new() { ["max"] = 255 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record ProfesionalSaludImportRow(string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<ProfesionalSaludImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = ProfesionalSalud.Create(row.NumeroRegistro, row.NombrePrescriptor, row.DireccionClinica);
            if (result.IsSuccess)
            {
                _ = context.ProfesionalesSalud.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

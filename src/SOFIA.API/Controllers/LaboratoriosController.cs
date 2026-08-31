using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;
using SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;
using SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorioById;
using SOFIA.Application.Laboratorios.Queries.GetLaboratorios;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LaboratoriosController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
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

        var existingNames = await context.Laboratorios
            .Select(l => l.NombreCompania.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var nombre = row.Values.GetValueOrDefault("NombreCompania");
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                if (!seenNames.ContainsKey(nombre))
                {
                    seenNames[nombre] = [];
                }

                seenNames[nombre].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var nombre = row.Values.GetValueOrDefault("NombreCompania");
                var codigo = row.Values.GetValueOrDefault("CodigoIdentificador");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreCompania"));
                }
                else
                {
                    if (nombre.Length > 150)
                    {
                        errors.Add(new ValidationError("max-length", "nombreCompania", new() { ["max"] = 150 }));
                    }

                    if (existingSet.Contains(nombre.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "nombreCompania", new() { ["value"] = nombre }));
                    }

                    if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "nombreCompania", new() { ["value"] = nombre }));
                    }
                }

                if (!string.IsNullOrEmpty(codigo) && codigo.Length > 50)
                {
                    errors.Add(new ValidationError("max-length", "codigoIdentificador", new() { ["max"] = 50 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record LaboratorioImportRow(string NombreCompania, string? CodigoIdentificador);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<LaboratorioImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = Laboratorio.Create(row.NombreCompania, row.CodigoIdentificador);
            if (result.IsSuccess)
            {
                _ = context.Laboratorios.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

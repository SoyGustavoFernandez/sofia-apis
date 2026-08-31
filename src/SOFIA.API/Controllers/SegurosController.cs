using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Seguros.Commands.CreateAseguradora;
using SOFIA.Application.Seguros.Commands.DeleteAseguradora;
using SOFIA.Application.Seguros.Commands.UpdateAseguradora;
using SOFIA.Application.Seguros.Queries.GetAseguradoraById;
using SOFIA.Application.Seguros.Queries.GetAseguradoras;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SegurosController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Seguros", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateAseguradora([FromBody] CreateAseguradoraCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguros", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAseguradoras([FromQuery] GetAseguradorasQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Seguros", "Leer")]
    public async Task<IActionResult> GetAseguradoraById(Guid id)
    {
        var result = await sender.Send(new GetAseguradoraByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Seguros", "Actualizar")]
    public async Task<IActionResult> UpdateAseguradora(Guid id, [FromBody] UpdateAseguradoraCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Seguros", "Eliminar")]
    public async Task<IActionResult> DeleteAseguradora(Guid id)
    {
        var result = await sender.Send(new DeleteAseguradoraCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "NombreComercial", "CodigoIdentificadorNacional" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-seguros.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "NombreComercial", "CodigoIdentificadorNacional" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingCodes = await context.Aseguradoras
            .Select(a => a.CodigoIdentificadorNacional.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var code = row.Values.GetValueOrDefault("CodigoIdentificadorNacional");
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (!seenCodes.ContainsKey(code))
                {
                    seenCodes[code] = [];
                }

                seenCodes[code].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var nombre = row.Values.GetValueOrDefault("NombreComercial");
                var codigo = row.Values.GetValueOrDefault("CodigoIdentificadorNacional");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreComercial"));
                }
                else if (nombre.Length > 150)
                {
                    errors.Add(new ValidationError("max-length", "nombreComercial", new() { ["max"] = 150 }));
                }

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    errors.Add(new ValidationError("required", "codigoIdentificadorNacional"));
                }
                else
                {
                    if (codigo.Length > 50)
                    {
                        errors.Add(new ValidationError("max-length", "codigoIdentificadorNacional", new() { ["max"] = 50 }));
                    }

                    if (existingSet.Contains(codigo.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "codigoIdentificadorNacional", new() { ["value"] = codigo }));
                    }

                    if (seenCodes.TryGetValue(codigo, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "codigoIdentificadorNacional", new() { ["value"] = codigo }));
                    }
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record AseguradoraImportRow(string NombreComercial, string CodigoIdentificadorNacional);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<AseguradoraImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = AseguradoraMedica.Create(row.NombreComercial, row.CodigoIdentificadorNacional);
            if (result.IsSuccess)
            {
                _ = context.Aseguradoras.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

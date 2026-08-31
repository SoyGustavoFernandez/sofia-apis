using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredienteActivoById;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredientesActivos;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IngredientesActivosController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetIngredientesActivosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetIngredienteActivoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngredienteActivoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIngredienteActivoCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteIngredienteActivoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "DenominacionDci", "CodigoAtc" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-ingredientesactivos.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "DenominacionDci", "CodigoAtc" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingCodes = await context.IngredientesActivos
            .Select(i => i.CodigoAtc.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var code = row.Values.GetValueOrDefault("CodigoAtc");
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
                var dci = row.Values.GetValueOrDefault("DenominacionDci");
                var atc = row.Values.GetValueOrDefault("CodigoAtc");

                if (string.IsNullOrWhiteSpace(dci))
                {
                    errors.Add(new ValidationError("required", "denominacionDci"));
                }
                else if (dci.Length > 255)
                {
                    errors.Add(new ValidationError("max-length", "denominacionDci", new() { ["max"] = 255 }));
                }

                if (string.IsNullOrWhiteSpace(atc))
                {
                    errors.Add(new ValidationError("required", "codigoAtc"));
                }
                else
                {
                    if (atc.Length > 15)
                    {
                        errors.Add(new ValidationError("max-length", "codigoAtc", new() { ["max"] = 15 }));
                    }

                    if (existingSet.Contains(atc.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "codigoAtc", new() { ["value"] = atc }));
                    }

                    if (seenCodes.TryGetValue(atc, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "codigoAtc", new() { ["value"] = atc }));
                    }
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record IngredienteActivoImportRow(string DenominacionDci, string CodigoAtc);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<IngredienteActivoImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = IngredienteActivo.Create(row.DenominacionDci, row.CodigoAtc);
            if (result.IsSuccess)
            {
                _ = context.IngredientesActivos.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

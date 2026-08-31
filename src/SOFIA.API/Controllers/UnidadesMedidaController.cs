using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadMedidaById;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UnidadesMedidaController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("UnidadesMedida", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetUnidadesMedidaQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetUnidadMedidaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnidadMedidaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnidadMedidaCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteUnidadMedidaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "Codigo", "Descripcion" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-unidadesmedida.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "Codigo", "Descripcion" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        // Load existing codes for duplicate check
        var existingCodes = await context.UnidadesMedida
            .Select(u => u.Codigo.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var codigo = row.Values.GetValueOrDefault("Codigo");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                if (!seenCodes.ContainsKey(codigo))
                {
                    seenCodes[codigo] = [];
                }

                seenCodes[codigo].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var codigo = row.Values.GetValueOrDefault("Codigo");
                var descripcion = row.Values.GetValueOrDefault("Descripcion");

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    errors.Add(new ValidationError("required", "codigo"));
                }
                else
                {
                    if (codigo.Length > 10)
                    {
                        errors.Add(new ValidationError("max-length", "codigo", new() { ["max"] = 10 }));
                    }

                    if (existingSet.Contains(codigo.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "codigo", new() { ["value"] = codigo }));
                    }

                    if (seenCodes.TryGetValue(codigo, out var rowsWithSameCode) && rowsWithSameCode.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "codigo", new() { ["value"] = codigo }));
                    }
                }

                if (string.IsNullOrWhiteSpace(descripcion))
                {
                    errors.Add(new ValidationError("required", "descripcion"));
                }
                else if (descripcion.Length > 50)
                {
                    errors.Add(new ValidationError("max-length", "descripcion", new() { ["max"] = 50 }));
                }

                return new PreviewRowResult
                {
                    RowNumber = row.RowNumber,
                    Data = row.Values,
                    Errors = errors
                };
            })]
        };

        return Ok(result);
    }

    public record UnidadMedidaImportRow(string Codigo, string Descripcion);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<UnidadMedidaImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = UnidadMedida.Create(row.Codigo, row.Descripcion);
            if (result.IsSuccess)
            {
                _ = context.UnidadesMedida.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

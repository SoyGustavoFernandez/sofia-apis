using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.CreateProveedor;
using SOFIA.Application.Proveedores.Commands.DeleteProveedor;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;
using SOFIA.Application.Proveedores.Commands.UpdateProveedor;
using SOFIA.Application.Proveedores.Queries.GetProveedorById;
using SOFIA.Application.Proveedores.Queries.GetProveedores;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProveedoresController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Proveedores", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateProveedor([FromBody] CreateProveedorCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Proveedores", "RegistrarPrecioProveedor")]
    [HttpPost("precios")]
    public async Task<IActionResult> RegistrarPrecioProveedor([FromBody] RegistrarPrecioProveedorCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Proveedores", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetProveedores([FromQuery] GetProveedoresQuery request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Proveedores", "Leer")]
    public async Task<IActionResult> GetProveedorById(Guid id)
    {
        var result = await sender.Send(new GetProveedorByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Proveedores", "Actualizar")]
    public async Task<IActionResult> UpdateProveedor(Guid id, [FromBody] UpdateProveedorCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Proveedores", "Eliminar")]
    public async Task<IActionResult> DeleteProveedor(Guid id)
    {
        var result = await sender.Send(new DeleteProveedorCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "RazonSocial", "TaxId", "TerminosFinancieros", "CalificacionEsg", "TasaCumplimiento" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-proveedores.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "RazonSocial", "TaxId", "TerminosFinancieros", "CalificacionEsg", "TasaCumplimiento" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingTaxIds = await context.Proveedores
            .Select(p => p.TaxId.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingTaxIds);

        var seenTaxIds = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var taxId = row.Values.GetValueOrDefault("TaxId");
            if (!string.IsNullOrWhiteSpace(taxId))
            {
                if (!seenTaxIds.ContainsKey(taxId))
                {
                    seenTaxIds[taxId] = [];
                }

                seenTaxIds[taxId].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var razon = row.Values.GetValueOrDefault("RazonSocial");
                var taxId = row.Values.GetValueOrDefault("TaxId");
                var terminos = row.Values.GetValueOrDefault("TerminosFinancieros");
                var esgStr = row.Values.GetValueOrDefault("CalificacionEsg");
                var tasaStr = row.Values.GetValueOrDefault("TasaCumplimiento");

                if (string.IsNullOrWhiteSpace(razon))
                {
                    errors.Add(new ValidationError("required", "razonSocial"));
                }
                else if (razon.Length > 200)
                {
                    errors.Add(new ValidationError("max-length", "razonSocial", new() { ["max"] = 200 }));
                }

                if (string.IsNullOrWhiteSpace(taxId))
                {
                    errors.Add(new ValidationError("required", "taxId"));
                }
                else
                {
                    if (taxId.Length > 50)
                    {
                        errors.Add(new ValidationError("max-length", "taxId", new() { ["max"] = 50 }));
                    }

                    if (existingSet.Contains(taxId.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "taxId", new() { ["value"] = taxId }));
                    }

                    if (seenTaxIds.TryGetValue(taxId, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "taxId", new() { ["value"] = taxId }));
                    }
                }

                if (!string.IsNullOrEmpty(terminos) && terminos.Length > 100)
                {
                    errors.Add(new ValidationError("max-length", "terminosFinancieros", new() { ["max"] = 100 }));
                }

                if (!string.IsNullOrEmpty(esgStr))
                {
                    if (!decimal.TryParse(esgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var esg))
                    {
                        errors.Add(new ValidationError("invalid-decimal", "calificacionEsg"));
                    }
                    else if (esg is < 0 or > 100)
                    {
                        errors.Add(new ValidationError("invalid-range", "calificacionEsg", new() { ["min"] = 0, ["max"] = 100 }));
                    }
                }

                if (string.IsNullOrWhiteSpace(tasaStr))
                {
                    errors.Add(new ValidationError("required", "tasaCumplimiento"));
                }
                else if (!decimal.TryParse(tasaStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tasa))
                {
                    errors.Add(new ValidationError("invalid-decimal", "tasaCumplimiento"));
                }
                else if (tasa is < 0 or > 100)
                {
                    errors.Add(new ValidationError("invalid-range", "tasaCumplimiento", new() { ["min"] = 0, ["max"] = 100 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record ProveedorImportRow(
        string RazonSocial,
        string TaxId,
        string? TerminosFinancieros,
        decimal? CalificacionEsg,
        decimal TasaCumplimiento);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<ProveedorImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = ProveedorDistribuidor.Create(row.RazonSocial, row.TaxId, row.TerminosFinancieros, row.CalificacionEsg, row.TasaCumplimiento);
            if (result.IsSuccess)
            {
                _ = context.Proveedores.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

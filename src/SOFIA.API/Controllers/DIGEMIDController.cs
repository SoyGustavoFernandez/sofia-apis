using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;
using SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;
using SOFIA.Application.DIGEMID.Commands.DeleteDigemidProducto;
using SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;
using SOFIA.Application.DIGEMID.Commands.UpdateDigemidProducto;
using SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogo;
using SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogoById;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DigemidController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("DIGEMID", "AislarLoteCuarentena")]
    [HttpPost("cuarentena")]
    public async Task<IActionResult> AislarLoteCuarentena([FromBody] AislarLoteCuarentenaCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "GenerarActaDestruccion")]
    [HttpPost("acta-destruccion")]
    public async Task<IActionResult> GenerarActaDestruccion([FromBody] GenerarActaDestruccionCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("cuarentena")]
    public async Task<IActionResult> GetCuarentena([FromQuery] Guid? sucursalId, [FromQuery] string? estadoResolucion, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.DIGEMID.Queries.GetCuarentena.GetCuarentenaQuery(sucursalId, estadoResolucion, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("actas")]
    public async Task<IActionResult> GetActas([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.DIGEMID.Queries.GetActas.GetActasQuery(fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("catalogo")]
    public async Task<IActionResult> GetCatalogo([FromQuery] GetDigemidCatalogoQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("catalogo/{id:guid}")]
    public async Task<IActionResult> GetCatalogoById(Guid id)
    {
        var result = await sender.Send(new GetDigemidCatalogoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("DIGEMID", "Crear")]
    [HttpPost("catalogo")]
    public async Task<IActionResult> CreateCatalogo([FromBody] CreateDigemidProductoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCatalogoById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("DIGEMID", "Actualizar")]
    [HttpPut("catalogo/{id:guid}")]
    public async Task<IActionResult> UpdateCatalogo(Guid id, [FromBody] UpdateDigemidProductoCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("DIGEMID", "Eliminar")]
    [HttpDelete("catalogo/{id:guid}")]
    public async Task<IActionResult> DeleteCatalogo(Guid id)
    {
        var result = await sender.Send(new DeleteDigemidProductoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("catalogo/plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "CodProd", "NomProd", "Concent", "FormaFarmaceutica", "Fraccion", "RegistroSanitario", "Titular", "Estado" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-digemid.xlsx");
    }

    [HttpPost("catalogo/previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "CodProd", "NomProd", "Concent", "FormaFarmaceutica", "Fraccion", "RegistroSanitario", "Titular", "Estado" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingCodes = await context.DigemidCatalogoProductos
            .Select(d => d.CodProd.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var cod = row.Values.GetValueOrDefault("CodProd");
            if (!string.IsNullOrWhiteSpace(cod))
            {
                if (!seenCodes.ContainsKey(cod))
                {
                    seenCodes[cod] = [];
                }

                seenCodes[cod].Add(row.RowNumber);
            }
        }

        var result = new PreviewResult
        {
            Rows = [.. rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var cod = row.Values.GetValueOrDefault("CodProd");
                var nom = row.Values.GetValueOrDefault("NomProd");
                var estado = row.Values.GetValueOrDefault("Estado");

                if (string.IsNullOrWhiteSpace(cod))
                {
                    errors.Add(new ValidationError("required", "codProd"));
                }
                else
                {
                    if (existingSet.Contains(cod.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "codProd", new() { ["value"] = cod }));
                    }

                    if (seenCodes.TryGetValue(cod, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "codProd", new() { ["value"] = cod }));
                    }
                }

                if (string.IsNullOrWhiteSpace(nom))
                {
                    errors.Add(new ValidationError("required", "nomProd"));
                }

                if (string.IsNullOrWhiteSpace(estado))
                {
                    errors.Add(new ValidationError("required", "estado"));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record DigemidImportRow(
        string CodProd,
        string NomProd,
        string? Concent,
        string? FormaFarmaceutica,
        string? Fraccion,
        string? RegistroSanitario,
        string? Titular,
        string Estado);

    [HttpPost("catalogo/carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<DigemidImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = DigemidCatalogoProducto.Create(
                row.CodProd,
                row.NomProd,
                row.Concent,
                row.FormaFarmaceutica,
                row.Fraccion,
                row.RegistroSanitario,
                row.Titular,
                row.Estado);

            if (result.IsSuccess)
            {
                _ = context.DigemidCatalogoProductos.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

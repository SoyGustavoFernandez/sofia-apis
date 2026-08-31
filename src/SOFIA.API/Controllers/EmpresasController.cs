using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empresas.Commands.DeleteEmpresa;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Application.Empresas.Commands.UpdateEmpresa;
using SOFIA.Application.Empresas.Queries.GetEmpresaById;
using SOFIA.Application.Empresas.Queries.GetEmpresas;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmpresasController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    /// <summary>
    /// Public registration: creates the company, main branch, admin user, and returns a JWT for immediate login.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEmpresaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? StatusCode(201, new { token = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EstadoEmpresa? estado)
    {
        var result = await sender.Send(new GetEmpresasQuery(estado));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetEmpresaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmpresaCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Empresas", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteEmpresaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "Nombre", "RUC" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-empresas.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "Nombre", "RUC" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingNames = await context.Empresas
            .Select(e => e.Nombre.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var nombre = row.Values.GetValueOrDefault("Nombre");
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
                var nombre = row.Values.GetValueOrDefault("Nombre");
                var ruc = row.Values.GetValueOrDefault("RUC");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombre"));
                }
                else
                {
                    if (existingSet.Contains(nombre.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "nombre", new() { ["value"] = nombre }));
                    }

                    if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "nombre", new() { ["value"] = nombre }));
                    }
                }

                if (!string.IsNullOrEmpty(ruc) && (ruc.Length != 11 || !ruc.All(char.IsDigit)))
                {
                    errors.Add(new ValidationError("invalid-digits", "ruc", new() { ["digits"] = 11 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record EmpresaImportRow(string Nombre, string? RUC);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<EmpresaImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = Empresa.Create(row.Nombre, row.RUC);
            if (result.IsSuccess)
            {
                _ = context.Empresas.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

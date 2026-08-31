using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.AssignPermission;
using SOFIA.Application.Security.Commands.Roles.AssignRol;
using SOFIA.Application.Security.Commands.Roles.CreateRol;
using SOFIA.Application.Security.Commands.Roles.DeleteRol;
using SOFIA.Application.Security.Commands.Roles.RemoveRol;
using SOFIA.Application.Security.Commands.Roles.RevokePermission;
using SOFIA.Application.Security.Commands.Roles.UpdateRol;
using SOFIA.Application.Security.Queries.Roles.GetPermissions;
using SOFIA.Application.Security.Queries.Roles.GetRoles;
using SOFIA.Application.Security.Queries.Roles.GetRolById;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RolesController(ISender sender, IApplicationDbContext context, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Seguridad", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await sender.Send(new GetRolesQuery());
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRolById(Guid id)
    {
        var result = await sender.Send(new GetRolByIdQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateRol([FromBody] CreateRolCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRoles), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRol(Guid id, [FromBody] UpdateRolRequest request)
    {
        var result = await sender.Send(new UpdateRolCommand(id, request.Descripcion, request.NivelJerarquia));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRol(Guid id)
    {
        var result = await sender.Send(new DeleteRolCommand(id));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "AsignarRoles")]
    [HttpPost("assign-to-user")]
    public async Task<IActionResult> AssignRol([FromBody] AssignRolToUserCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "AsignarRoles")]
    [HttpPost("remove-from-user")]
    public async Task<IActionResult> RemoveRol([FromBody] RemoveRolFromUserCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Leer")]
    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid id)
    {
        var result = await sender.Send(new GetPermissionsByRolQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "GestionarPermisos")]
    [HttpPost("permissions")]
    public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionToRolCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok(new { Id = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "GestionarPermisos")]
    [HttpDelete("permissions/{permisoId:guid}")]
    public async Task<IActionResult> RevokePermission(Guid permisoId)
    {
        var result = await sender.Send(new RevokePermissionFromRolCommand(permisoId));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "NombreRol", "Descripcion", "NivelJerarquia" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-roles.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "NombreRol", "Descripcion", "NivelJerarquia" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);

        var existingNames = await context.Roles
            .Select(r => r.NombreRol.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var nombre = row.Values.GetValueOrDefault("NombreRol");
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
                var nombre = row.Values.GetValueOrDefault("NombreRol");
                var nivelStr = row.Values.GetValueOrDefault("NivelJerarquia");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreRol"));
                }
                else
                {
                    if (nombre.Length > 50)
                    {
                        errors.Add(new ValidationError("max-length", "nombreRol", new() { ["max"] = 50 }));
                    }

                    if (existingSet.Contains(nombre.ToLower()))
                    {
                        errors.Add(new ValidationError("duplicate", "nombreRol", new() { ["value"] = nombre }));
                    }

                    if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1)
                    {
                        errors.Add(new ValidationError("duplicate-in-file", "nombreRol", new() { ["value"] = nombre }));
                    }
                }

                if (string.IsNullOrWhiteSpace(nivelStr))
                {
                    errors.Add(new ValidationError("required", "nivelJerarquia"));
                }
                else if (!int.TryParse(nivelStr, out var nivel))
                {
                    errors.Add(new ValidationError("invalid-integer", "nivelJerarquia"));
                }
                else if (nivel < 1)
                {
                    errors.Add(new ValidationError("min-value", "nivelJerarquia", new() { ["min"] = 1 }));
                }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };

        return Ok(result);
    }

    public record RolImportRow(string NombreRol, string? Descripcion, int NivelJerarquia);

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<RolImportRow> rows, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in rows)
        {
            var result = Rol.Create(row.NombreRol, row.Descripcion, row.NivelJerarquia);
            if (result.IsSuccess)
            {
                _ = context.Roles.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Ok(new { savedCount = saved });
    }
}

public record UpdateRolRequest(string? Descripcion, [property: System.Text.Json.Serialization.JsonRequired] int NivelJerarquia);

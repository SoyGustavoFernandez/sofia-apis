using System.Reflection;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Security.Commands.Roles.AssignPermission;
using SOFIA.Application.Security.Commands.Roles.AssignRol;
using SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;
using SOFIA.Application.Security.Commands.Roles.CreateRol;
using SOFIA.Application.Security.Commands.Roles.DeleteRol;
using SOFIA.Application.Security.Commands.Roles.RemoveRol;
using SOFIA.Application.Security.Commands.Roles.RevokePermission;
using SOFIA.Application.Security.Commands.Roles.UpdateRol;
using SOFIA.Application.Security.Commands.Roles.SetSucursales;
using SOFIA.Application.Security.Queries.Roles.GetPermissions;
using SOFIA.Application.Security.Queries.Roles.GetRoles;
using SOFIA.Application.Security.Queries.Roles.GetRolById;
using SOFIA.Application.Security.Queries.Roles.GetRolSucursales;
using SOFIA.Application.Security.Queries.Roles.PreviewImportRoles;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RolesController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Seguridad", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetRoles(
        [FromQuery] string? nombreRol,
        [FromQuery] string? descripcion,
        [FromQuery] int? nivelJerarquiaDesde,
        [FromQuery] int? nivelJerarquiaHasta,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await sender.Send(new GetRolesQuery
        {
            NombreRol = nombreRol,
            Descripcion = descripcion,
            NivelJerarquiaDesde = nivelJerarquiaDesde,
            NivelJerarquiaHasta = nivelJerarquiaHasta,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
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
    [HttpGet("permissions/catalog")]
    public IActionResult GetPermissionsCatalog()
    {
        var catalog = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(m => m.GetCustomAttributes<HasPermissionAttribute>()))
            .GroupBy(a => a.Module, a => a.Action)
            .Select(g => new { Modulo = g.Key, Acciones = g.Distinct().OrderBy(a => a).ToList() })
            .OrderBy(g => g.Modulo)
            .ToList();
        return Ok(catalog);
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

    [HasPermission("Seguridad", "Leer")]
    [HttpGet("{id:guid}/sucursales")]
    public async Task<IActionResult> GetRolSucursales(Guid id)
    {
        var result = await sender.Send(new GetRolSucursalesQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "GestionarPermisos")]
    [HttpPut("{id:guid}/sucursales")]
    public async Task<IActionResult> SetRolSucursales(Guid id, [FromBody] SetRolSucursalesRequest request)
    {
        var result = await sender.Send(new SetRolSucursalesCommand(id, request.SucursalIds));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Seguridad", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] RolExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery
        {
            NombreRol = request.NombreRol,
            Descripcion = request.Descripcion,
            NivelJerarquiaDesde = request.NivelJerarquiaDesde,
            NivelJerarquiaHasta = request.NivelJerarquiaHasta,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(r => new object?[]
        {
            r.NombreRol,
            r.Descripcion,
            r.NivelJerarquia,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "roles.xlsx");
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
        var result = await sender.Send(new PreviewImportRolesQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<RolImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaRolesCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record UpdateRolRequest(string? Descripcion, [property: System.Text.Json.Serialization.JsonRequired] int NivelJerarquia);
public record SetRolSucursalesRequest(List<Guid> SucursalIds);

public record RolExportRequest(string[] Headers, string? NombreRol, string? Descripcion, int? NivelJerarquiaDesde, int? NivelJerarquiaHasta);

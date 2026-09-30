using Microsoft.AspNetCore.RateLimiting;
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
public class EmpresasController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Public registration: creates the company, main branch, admin user, and returns a JWT for immediate login.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.SignUpPolicy)]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEmpresaCommand command)
    {
        var result = await sender.Send(command);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        Response.AppendRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return StatusCode(201, new { token = result.Value.AccessToken });
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre,
        [FromQuery] EstadoEmpresa? estado,
        [FromQuery] DateTimeOffset? fechaVencimientoDesde,
        [FromQuery] DateTimeOffset? fechaVencimientoHasta,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await sender.Send(new GetEmpresasQuery
        {
            Nombre = nombre,
            Estado = estado,
            FechaVencimientoDesde = fechaVencimientoDesde,
            FechaVencimientoHasta = fechaVencimientoHasta,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return result.ToActionResult();
    }

    [HasPermission("Empresas", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetEmpresaByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Empresas", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmpresaCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HasPermission("Empresas", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] EmpresaExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEmpresasQuery
        {
            Nombre = request.Nombre,
            Estado = request.Estado,
            FechaVencimientoDesde = request.FechaVencimientoDesde,
            FechaVencimientoHasta = request.FechaVencimientoHasta,
            PageSize = PaginationLimits.MaxPageSize,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        if (result.Value.ExceedsExportLimit())
        {
            return ExportExtensions.TooManyRowsResult();
        }

        var rows = result.Value.Items.Select(e => new object?[]
        {
            e.Nombre,
            e.RUC,
            e.Estado.ToString(),
            e.EstaVigente ? request.YesLabel : request.NoLabel,
            e.FechaInicioTrial.ToString("dd/MM/yyyy"),
            e.FechaVencimiento.ToString("dd/MM/yyyy"),
            e.CantidadSucursales,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "empresas.xlsx");
    }

    public record EmpresaExportRequest([ExportHeaders] string[] Headers, [ExportLabel] string YesLabel, [ExportLabel] string NoLabel, string? Nombre, EstadoEmpresa? Estado, DateTimeOffset? FechaVencimientoDesde, DateTimeOffset? FechaVencimientoHasta);
}

using SOFIA.Application.Common.Excel;
using SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;
using SOFIA.Application.Proveedores.Commands.CreateProveedor;
using SOFIA.Application.Proveedores.Commands.DeleteProveedor;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;
using SOFIA.Application.Proveedores.Commands.UpdateProveedor;
using SOFIA.Application.Proveedores.Queries.GetProveedorById;
using SOFIA.Application.Proveedores.Queries.GetProveedores;
using SOFIA.Application.Proveedores.Queries.PreviewImportProveedores;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProveedoresController(ISender sender, IExcelReaderService excelReader) : ControllerBase
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

    [HasPermission("Proveedores", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] ProveedorExportRequest request)
    {
        var result = await sender.Send(new GetProveedoresQuery
        {
            RazonSocial = request.RazonSocial,
            TaxId = request.TaxId,
            TasaCumplimientoDesde = request.TasaCumplimientoDesde,
            TasaCumplimientoHasta = request.TasaCumplimientoHasta,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(p => new object?[]
        {
            p.RazonSocial,
            p.TaxId,
            p.TasaCumplimiento,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "proveedores.xlsx");
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
        var result = await sender.Send(new PreviewImportProveedoresQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<ProveedorImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaProveedoresCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record ProveedorExportRequest(string[] Headers, string? RazonSocial, string? TaxId, decimal? TasaCumplimientoDesde, decimal? TasaCumplimientoHasta);

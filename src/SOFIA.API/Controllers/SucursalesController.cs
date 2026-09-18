using SOFIA.Application.Common.Excel;
using SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;
using SOFIA.Application.Sucursales.Commands.CreateSucursal;
using SOFIA.Application.Sucursales.Commands.DeleteSucursal;
using SOFIA.Application.Sucursales.Commands.UpdateSucursal;
using SOFIA.Application.Sucursales.Queries.GetById;
using SOFIA.Application.Sucursales.Queries.GetSucursalesWithPagination;
using SOFIA.Application.Sucursales.Queries.PreviewImportSucursales;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SucursalesController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    private static readonly string[] Columns = ["Nombre", "DireccionFisica", "NumeroLicencia"];

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var bytes = ExcelTemplateGenerator.GenerateTemplate(Columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-sucursales.xlsx");
    }

    [HasPermission("Sucursales", "Crear")]
    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, Columns);
        var result = await sender.Send(new PreviewImportSucursalesQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HasPermission("Sucursales", "Crear")]
    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<SucursalImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaSucursalesCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : result.ToProblemResult();
    }

    [HasPermission("Sucursales", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetSucursalesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Sucursales", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetSucursalByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Sucursales", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] SucursalExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSucursalesWithPaginationQuery
        {
            Nombre = request.Nombre,
            NumeroLicencia = request.NumeroLicencia,
            DireccionFisica = request.DireccionFisica,
            PageSize = int.MaxValue,
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(s => new object?[]
        {
            s.Nombre,
            s.DireccionFisica,
            s.NumeroLicencia,
            s.GerenteNombre,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sucursales.xlsx");
    }

    [HasPermission("Sucursales", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSucursalCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Sucursales", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSucursalCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HasPermission("Sucursales", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteSucursalCommand(id));
        return result.ToActionResult();
    }
}

public record SucursalExportRequest(
    string[] Headers,
    string? Nombre,
    string? NumeroLicencia,
    string? DireccionFisica);

using SOFIA.Application.Common.Excel;
using SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;
using SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;
using SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;
using SOFIA.Application.DIGEMID.Commands.DeleteDigemidProducto;
using SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;
using SOFIA.Application.DIGEMID.Commands.UpdateDigemidProducto;
using SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogo;
using SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogoById;
using SOFIA.Application.DIGEMID.Queries.PreviewImportDigemid;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DigemidController(ISender sender, IExcelReaderService excelReader) : ControllerBase
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
        var result = await sender.Send(new PreviewImportDigemidQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("catalogo/carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<DigemidImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaDigemidCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpPost("catalogo/exportar")]
    public async Task<IActionResult> ExportarCatalogo([FromBody] DigemidCatalogoExportRequest request)
    {
        var result = await sender.Send(new GetDigemidCatalogoQuery
        {
            CodProd = request.CodProd,
            NomProd = request.NomProd,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(p => new object?[]
        {
            p.CodProd,
            p.NomProd,
            p.FormaFarmaceutica,
            p.Estado,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "digemid-catalogo.xlsx");
    }
}

public record DigemidCatalogoExportRequest(
    string[] Headers,
    string? CodProd,
    string? NomProd);

using SOFIA.Application.Common.Excel;
using SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;
using SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiaUoMById;
using SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiasUoM;
using SOFIA.Application.JerarquiasUoM.Queries.GetUnidadesVendiblesUoM;
using SOFIA.Application.JerarquiasUoM.Queries.PreviewImportJerarquiasUoM;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class JerarquiasUoMController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    private static readonly string[] ImportColumns = ["Producto", "UnidadMayor", "UnidadMenor", "Multiplicador"];

    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetJerarquiasUoMQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpGet("unidades-vendibles")]
    public async Task<IActionResult> GetUnidadesVendibles([FromQuery] Guid productoId)
    {
        var result = await sender.Send(new GetUnidadesVendiblesUoMQuery(productoId));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetJerarquiaUoMByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJerarquiaUoMCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJerarquiaUoMCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteJerarquiaUoMCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("JerarquiasUoM", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] JerarquiaUoMExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetJerarquiasUoMQuery
        {
            ProductoNombre = request.ProductoNombre,
            UnidadMayorNombre = request.UnidadMayorNombre,
            UnidadMenorNombre = request.UnidadMenorNombre,
            MultiplicadorMin = request.MultiplicadorMin,
            MultiplicadorMax = request.MultiplicadorMax,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(j => new object?[]
        {
            j.ProductoNombre,
            j.UnidadMayorNombre,
            j.UnidadMenorNombre,
            j.Multiplicador,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "jerarquias-uom.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var bytes = ExcelTemplateGenerator.GenerateTemplate(ImportColumns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-jerarquias-uom.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, ImportColumns);
        var result = await sender.Send(new PreviewImportJerarquiasUoMQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<JerarquiaUoMImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaJerarquiasUoMCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record JerarquiaUoMExportRequest(
    string[] Headers,
    string? ProductoNombre,
    string? UnidadMayorNombre,
    string? UnidadMenorNombre,
    decimal? MultiplicadorMin,
    decimal? MultiplicadorMax);

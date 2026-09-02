using SOFIA.Application.Common.Excel;
using SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;
using SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;
using SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadMedidaById;
using SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;
using SOFIA.Application.UnidadesMedida.Queries.PreviewImportUnidadesMedida;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UnidadesMedidaController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("UnidadesMedida", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetUnidadesMedidaQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetUnidadMedidaByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnidadMedidaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnidadMedidaCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteUnidadMedidaCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "Codigo", "Descripcion" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-unidadesmedida.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "Codigo", "Descripcion" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportUnidadesMedidaQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<UnidadMedidaImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaUnidadesMedidaCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("UnidadesMedida", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] UnidadMedidaExportRequest request)
    {
        var result = await sender.Send(new GetUnidadesMedidaQuery
        {
            Codigo = request.Codigo,
            Descripcion = request.Descripcion,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(u => new object?[]
        {
            u.Codigo,
            u.Descripcion,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "unidadesmedida.xlsx");
    }
}

public record UnidadMedidaExportRequest(
    string[] Headers,
    string? Codigo,
    string? Descripcion);

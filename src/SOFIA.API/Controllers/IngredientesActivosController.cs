using SOFIA.Application.Common.Excel;
using SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;
using SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredienteActivoById;
using SOFIA.Application.IngredientesActivos.Queries.GetIngredientesActivos;
using SOFIA.Application.IngredientesActivos.Queries.PreviewImportIngredientesActivos;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IngredientesActivosController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetPaginated([FromQuery] GetIngredientesActivosQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetIngredienteActivoByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngredienteActivoCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIngredienteActivoCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteIngredienteActivoCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("IngredientesActivos", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] IngredienteActivoExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetIngredientesActivosQuery
        {
            DenominacionDci = request.DenominacionDci,
            CodigoAtc = request.CodigoAtc,
            PageSize = int.MaxValue,
        }, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error.Message, statusCode: result.StatusCode);
        }

        var rows = result.Value.Items.Select(i => new object?[]
        {
            i.DenominacionDci,
            i.CodigoAtc,
        });

        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ingredientes-activos.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "DenominacionDci", "CodigoAtc" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-ingredientesactivos.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "DenominacionDci", "CodigoAtc" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportIngredientesActivosQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<IngredienteActivoImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaIngredientesActivosCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record IngredienteActivoExportRequest(string[] Headers, string? DenominacionDci, string? CodigoAtc);

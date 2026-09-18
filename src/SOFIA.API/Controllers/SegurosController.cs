using SOFIA.Application.Common.Excel;
using SOFIA.Application.Seguros.Commands.CargaMasivaSeguros;
using SOFIA.Application.Seguros.Commands.CreateAseguradora;
using SOFIA.Application.Seguros.Commands.DeleteAseguradora;
using SOFIA.Application.Seguros.Commands.UpdateAseguradora;
using SOFIA.Application.Seguros.Queries.GetAseguradoraById;
using SOFIA.Application.Seguros.Queries.GetAseguradoras;
using SOFIA.Application.Seguros.Queries.PreviewImportSeguros;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SegurosController(ISender sender, IExcelReaderService excelReader) : ControllerBase
{
    [HasPermission("Seguros", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateAseguradora([FromBody] CreateAseguradoraCommand request)
    {
        var result = await sender.Send(request);
        return result.ToActionResult();
    }

    [HasPermission("Seguros", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetAseguradoras([FromQuery] GetAseguradorasQuery request)
    {
        var result = await sender.Send(request);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Seguros", "Leer")]
    public async Task<IActionResult> GetAseguradoraById(Guid id)
    {
        var result = await sender.Send(new GetAseguradoraByIdQuery(id));
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Seguros", "Actualizar")]
    public async Task<IActionResult> UpdateAseguradora(Guid id, [FromBody] UpdateAseguradoraCommand command)
    {
        command = command with { Id = id };
        var result = await sender.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Seguros", "Eliminar")]
    public async Task<IActionResult> DeleteAseguradora(Guid id)
    {
        var result = await sender.Send(new DeleteAseguradoraCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Seguros", "Leer")]
    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar([FromBody] AseguradoraExportRequest request)
    {
        var result = await sender.Send(new GetAseguradorasQuery
        {
            NombreComercial = request.NombreComercial,
            CodigoIdentificadorNacional = request.CodigoIdentificadorNacional,
            PageSize = int.MaxValue,
        });
        if (!result.IsSuccess)
        {
            return result.ToProblemResult();
        }

        var rows = result.Value.Items.Select(a => new object?[]
        {
            a.NombreComercial,
            a.CodigoIdentificadorNacional,
        });
        var bytes = ExcelTemplateGenerator.GenerateReport(request.Headers, rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "aseguradoras.xlsx");
    }

    [HttpGet("plantilla")]
    public IActionResult GetPlantilla()
    {
        var columns = new[] { "NombreComercial", "CodigoIdentificadorNacional" };
        var bytes = ExcelTemplateGenerator.GenerateTemplate(columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-seguros.xlsx");
    }

    [HttpPost("previsualizar")]
    public async Task<IActionResult> Previsualizar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo Excel.");
        }

        var columns = new[] { "NombreComercial", "CodigoIdentificadorNacional" };
        using var stream = file.OpenReadStream();
        var rows = excelReader.ReadRows(stream, columns);
        var result = await sender.Send(new PreviewImportSegurosQuery(rows), cancellationToken);
        return Ok(result);
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva([FromBody] List<SeguroImportRow> rows, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CargaMasivaSegurosCommand(rows), cancellationToken);
        return result.IsSuccess ? Ok(new { savedCount = result.Value }) : result.ToProblemResult();
    }
}

public record AseguradoraExportRequest(string[] Headers, string? NombreComercial, string? CodigoIdentificadorNacional);

using SOFIA.Application.Inventarios.Commands.CreateLote;
using SOFIA.Application.Inventarios.Commands.DeleteLote;
using SOFIA.Application.Inventarios.Commands.UpdateLote;
using SOFIA.Application.Inventarios.Queries.GetLoteById;
using SOFIA.Application.Inventarios.Queries.GetLotes;
using SOFIA.Application.Inventarios.Queries.GetLotesByProducto;
using SOFIA.Application.Inventarios.Commands.RegisterInventario;
using SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LotesInventarioController(ISender sender) : ControllerBase
{
    [HasPermission("Inventarios", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetLotesQuery query)
    {
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetLoteByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpGet("producto/{productoId:guid}")]
    public async Task<IActionResult> GetByProducto(Guid productoId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await sender.Send(new GetLotesByProductoQuery(productoId)
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoteInventarioCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Actualizar")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoteInventarioCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var result = await sender.Send(command);
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Eliminar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await sender.Send(new DeleteLoteInventarioCommand(id));
        return result.IsSuccess ? NoContent() : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Actualizar")]
    [HttpPost("stock")]
    public async Task<IActionResult> RegisterStock([FromBody] RegisterInventarioCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpGet("stock/medicamento/{medicamentoId:guid}")]
    public async Task<IActionResult> GetStockByMedicamento(Guid medicamentoId)
    {
        var result = await sender.Send(new GetStockByMedicamentoQuery(medicamentoId));
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

using SOFIA.Application.Transferencias.Commands.CreateTransferencia;
using SOFIA.Application.Transferencias.Commands.DespacharTransferencia;
using SOFIA.Application.Transferencias.Commands.RecibirTransferencia;
using SOFIA.Application.Transferencias.Commands.CancelarTransferencia;
using SOFIA.Application.Transferencias.Queries.GetTransferenciaById;
using SOFIA.Application.Transferencias.Queries.GetTransferencias;

namespace SOFIA.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class TransferenciasController(ISender sender) : ControllerBase
{
    [HasPermission("Transferencias", "Leer")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetTransferenciasQuery query)
    {
        var result = await sender.Send(query);
        return result.ToActionResult();
    }

    [HasPermission("Transferencias", "Leer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetTransferenciaByIdQuery(id));
        return result.ToActionResult();
    }

    [HasPermission("Transferencias", "Crear")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferenciaCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult();
    }

    [HasPermission("Transferencias", "Actualizar")]
    [HttpPut("{id:guid}/despachar")]
    public async Task<IActionResult> Despachar(Guid id)
    {
        var result = await sender.Send(new DespacharTransferenciaCommand(id));
        return result.ToActionResult();
    }

    [HasPermission("Transferencias", "Actualizar")]
    [HttpPut("{id:guid}/recibir")]
    public async Task<IActionResult> Recibir(Guid id, [FromBody] List<RecepcionLoteInputDto> recepciones)
    {
        var result = await sender.Send(new RecibirTransferenciaCommand(id, recepciones));
        return result.ToActionResult();
    }

    [HasPermission("Transferencias", "Actualizar")]
    [HttpPut("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var result = await sender.Send(new CancelarTransferenciaCommand(id));
        return result.ToActionResult();
    }
}

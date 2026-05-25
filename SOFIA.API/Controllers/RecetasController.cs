using SOFIA.Application.Recetas.Commands.DeleteReceta;
using SOFIA.Application.Recetas.Commands.UpdateReceta;
using SOFIA.Application.Recetas.Queries.GetRecetaById;
using SOFIA.Application.Recetas.Commands.CreateReceta;
using SOFIA.Application.Recetas.Queries.AnalizarReceta;

namespace SOFIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetasController(ISender sender) : ControllerBase
{

    [HasPermission("Recetas", "Crear")]
    [HttpPost]
    public async Task<IActionResult> CreateReceta([FromBody] CreateRecetaCommand request)
    {
        var result = await sender.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }



    [HasPermission("Recetas", "Leer")]
    [HttpGet]
    public async Task<IActionResult> GetRecetas([FromQuery] Guid? clienteId, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new Application.Recetas.Queries.GetRecetas.GetRecetasQuery(clienteId, fechaInicio, fechaFin, pageNumber, pageSize);
        var result = await sender.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }



    [HttpGet("{id}")]
    [HasPermission("Recetas", "Leer")]
    public async Task<IActionResult> GetRecetaById(Guid id)
    {
        var result = await sender.Send(new GetRecetaByIdQuery(id));
        return !result.IsSuccess ? NotFound(result) : Ok(result);
    }

    [HttpPost("analizar")]
    [HasPermission("Recetas", "Analizar")]
    public async Task<IActionResult> Analizar(IFormFile imagen, [FromQuery] string? especialidadContexto)
    {
        if (imagen == null || imagen.Length == 0)
        {
            return BadRequest("La imagen es requerida.");
        }

        using var stream = imagen.OpenReadStream();
        var query = new AnalizarRecetaQuery(stream, especialidadContexto);
        var result = await sender.Send(query);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    [HasPermission("Recetas", "Actualizar")]
    public async Task<IActionResult> UpdateReceta(Guid id, [FromBody] UpdateRecetaCommand command)
    {
        command = command with { Id = id };
        if (id != command.Id)
        {
            return BadRequest("El ID de la ruta no coincide con el comando.");
        }

        var result = await sender.Send(command);
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id}")]
    [HasPermission("Recetas", "Eliminar")]
    public async Task<IActionResult> DeleteReceta(Guid id)
    {
        var result = await sender.Send(new DeleteRecetaCommand(id));
        return !result.IsSuccess ? BadRequest(result) : Ok(result);
    }

}

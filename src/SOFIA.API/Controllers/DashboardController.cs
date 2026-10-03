using SOFIA.Application.Dashboard.Queries.GetDashboardCaja;
using SOFIA.Application.Dashboard.Queries.GetDashboardCuarentena;
using SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;
using SOFIA.Application.Dashboard.Queries.GetDashboardInventario;
using SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

namespace SOFIA.API.Controllers;

// One endpoint per permission module, so the client only requests the sections its user may read
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DashboardController(ISender sender) : ControllerBase
{
    [HasPermission("Ventas", "Leer")]
    [HttpGet("ventas")]
    public async Task<IActionResult> GetVentas([FromQuery] DateTimeOffset hoy)
    {
        var result = await sender.Send(new GetDashboardVentasQuery(hoy));

        return result.ToActionResult();
    }

    [HasPermission("POS", "Leer")]
    [HttpGet("caja")]
    public async Task<IActionResult> GetCaja()
    {
        var result = await sender.Send(new GetDashboardCajaQuery());

        return result.ToActionResult();
    }

    [HasPermission("Inventarios", "Leer")]
    [HttpGet("inventario")]
    public async Task<IActionResult> GetInventario([FromQuery] DateTimeOffset hoy)
    {
        var result = await sender.Send(new GetDashboardInventarioQuery(hoy));

        return result.ToActionResult();
    }

    [HasPermission("DIGEMID", "Leer")]
    [HttpGet("cuarentena")]
    public async Task<IActionResult> GetCuarentena()
    {
        var result = await sender.Send(new GetDashboardCuarentenaQuery());

        return result.ToActionResult();
    }

    [HasPermission("Devoluciones", "Leer")]
    [HttpGet("devoluciones")]
    public async Task<IActionResult> GetDevoluciones([FromQuery] DateTimeOffset hoy)
    {
        var result = await sender.Send(new GetDashboardDevolucionesQuery(hoy));

        return result.ToActionResult();
    }
}

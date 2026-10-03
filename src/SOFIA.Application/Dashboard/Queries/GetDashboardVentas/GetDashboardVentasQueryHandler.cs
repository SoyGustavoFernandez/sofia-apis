using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

public class GetDashboardVentasQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess, ICurrentUser currentUser)
    : IRequestHandler<GetDashboardVentasQuery, Result<DashboardVentasDto>>
{
    private const int DiasTendencia = 7;
    private const int CantidadTopProductos = 5;

    public async Task<Result<DashboardVentasDto>> Handle(GetDashboardVentasQuery request, CancellationToken cancellationToken)
    {
        var scope = await DashboardScopeResolver.ResolveAsync(context, sucursalAccess, cancellationToken);
        var ids = scope.Ids;
        var ventanas = DashboardDias.Ventanas(request.Hoy, DiasTendencia);
        var ventanaHoy = ventanas[^1];

        // Same rule as the cash count: a sale counts once collected and while it is not voided; amounts are gross
        var cobradas = context.Ventas
            .AsNoTracking()
            .Where(v => ids.Contains(v.SucursalId) && v.Estado != EstadoVenta.Anulada && v.Estado != EstadoVenta.Pendiente);

        var tendencia = new List<VentasDiaDto>(DiasTendencia);
        List<ResumenSucursal> hoyPorSucursal = [];
        foreach (var ventana in ventanas)
        {
            hoyPorSucursal = await ResumirPorSucursalAsync(EnVentana(cobradas, ventana), cancellationToken);
            tendencia.Add(Totalizar(ventana, hoyPorSucursal));
        }

        var hoy = tendencia[^1];
        var ayer = tendencia[^2];

        // Parked sales stay pending until charged, whatever day they were created
        var pendientes = await ResumirPorSucursalAsync(
            context.Ventas.AsNoTracking().Where(v => ids.Contains(v.SucursalId) && v.Estado == EstadoVenta.Pendiente),
            cancellationToken);

        var dto = new DashboardVentasDto(
            scope.MultiSucursal,
            scope.EmpresaCompleta,
            hoy,
            ayer,
            hoy.Comprobantes == 0 ? 0m : Math.Round(hoy.Monto / hoy.Comprobantes, 2),
            await GetMiasAsync(EnVentana(cobradas, ventanaHoy), ventanaHoy, cancellationToken),
            await GetPorMetodoPagoAsync(EnVentana(cobradas, ventanaHoy), cancellationToken),
            new VentasPendientesDto(pendientes.Sum(p => p.Comprobantes), pendientes.Sum(p => p.Monto)),
            scope.MultiSucursal ? PorSucursal(scope, hoyPorSucursal) : [],
            tendencia,
            await GetTopProductosAsync(cobradas, ventanas[0], ventanaHoy, cancellationToken));

        return Result.Success(dto);
    }

    // Bounds are captured UTC values and the upper one is exclusive, so adjacent days never share a sale
    private static IQueryable<Venta> EnVentana(IQueryable<Venta> ventas, VentanaDia ventana)
    {
        var desde = ventana.DesdeUtc;
        var hasta = ventana.HastaUtc;
        return ventas.Where(v => v.FechaHoraUtc >= desde && v.FechaHoraUtc < hasta);
    }

    private static async Task<List<ResumenSucursal>> ResumirPorSucursalAsync(IQueryable<Venta> ventas, CancellationToken cancellationToken) =>
        await ventas
            .GroupBy(v => v.SucursalId)
            .Select(g => new ResumenSucursal(g.Key, g.Sum(v => v.MontoTotalBruto), g.Count()))
            .ToListAsync(cancellationToken);

    private static VentasDiaDto Totalizar(VentanaDia ventana, List<ResumenSucursal> porSucursal) =>
        new(ventana.Desde, porSucursal.Sum(r => r.Monto), porSucursal.Sum(r => r.Comprobantes));

    private static List<VentaSucursalDto> PorSucursal(DashboardScope scope, List<ResumenSucursal> hoyPorSucursal)
    {
        var porId = hoyPorSucursal.ToDictionary(r => r.SucursalId);
        return [.. scope.Sucursales.Select(s => porId.TryGetValue(s.Id, out var r)
            ? new VentaSucursalDto(s.Id, s.Nombre, r.Monto, r.Comprobantes)
            : new VentaSucursalDto(s.Id, s.Nombre, 0m, 0))];
    }

    // Personal figures need an employee in the token; without one they are simply omitted
    private async Task<VentasDiaDto?> GetMiasAsync(IQueryable<Venta> cobradasHoy, VentanaDia ventanaHoy, CancellationToken cancellationToken)
    {
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return null;
        }

        var empleadoId = empleadoResult.Value;
        var mias = await ResumirPorSucursalAsync(cobradasHoy.Where(v => v.EmpleadoId == empleadoId), cancellationToken);
        return Totalizar(ventanaHoy, mias);
    }

    private async Task<List<VentaMetodoPagoDto>> GetPorMetodoPagoAsync(IQueryable<Venta> cobradasHoy, CancellationToken cancellationToken)
    {
        var ventas = await cobradasHoy
            .Select(v => new { v.Id, v.MontoTotalBruto })
            .ToListAsync(cancellationToken);

        // Joined through the counted sales because voiding a sale keeps its payment rows
        var pagos = await (
            from p in context.VentasPagos.AsNoTracking()
            join v in cobradasHoy on p.TransaccionId equals v.Id
            select new PagoCobrado(p.TransaccionId, p.MetodoPago, p.MontoPagado))
            .ToListAsync(cancellationToken);

        var coberturas = await (
            from r in context.VentasReclamosSeguro.AsNoTracking()
            join d in context.DetallesVenta.AsNoTracking() on r.DetalleVentaId equals d.Id
            join v in cobradasHoy on d.VentaId equals v.Id
            select new { d.VentaId, r.MontoCubierto })
            .ToListAsync(cancellationToken);

        var coberturaPorVenta = coberturas
            .GroupBy(c => c.VentaId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.MontoCubierto));

        var totales = PagosPorMetodoCalculator.Calcular(
            ventas.Select(v => new VentaCobrada(v.Id, v.MontoTotalBruto - coberturaPorVenta.GetValueOrDefault(v.Id))),
            pagos);

        return [.. Enum.GetValues<MetodoPago>().Select(m => new VentaMetodoPagoDto(m, totales[m]))];
    }

    private async Task<List<TopProductoDto>> GetTopProductosAsync(IQueryable<Venta> cobradas, VentanaDia primera, VentanaDia ultima, CancellationToken cancellationToken)
    {
        var desde = primera.DesdeUtc;
        var hasta = ultima.HastaUtc;

        var top = await cobradas
            .Where(v => v.FechaHoraUtc >= desde && v.FechaHoraUtc < hasta)
            .SelectMany(v => v.Detalles)
            .Where(d => d.Lote != null && d.Lote.Producto != null)
            .GroupBy(d => d.Lote!.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Unidades = g.Sum(d => d.CantidadVendida),
                Monto = g.Sum(d => d.CantidadVendida * d.PrecioFijadoUnidad),
            })
            .OrderByDescending(x => x.Unidades)
            .ThenBy(x => x.ProductoId)
            .Take(CantidadTopProductos)
            .ToListAsync(cancellationToken);

        // Names come from a second query: a navigation cannot be projected next to the aggregates of a GroupBy
        var productoIds = top.Select(x => x.ProductoId).ToList();
        var nombres = await context.Medicamentos
            .AsNoTracking()
            .Where(m => productoIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.NombreComercial, cancellationToken);

        return [.. top.Select(x => new TopProductoDto(x.ProductoId, nombres.GetValueOrDefault(x.ProductoId, string.Empty), x.Unidades, Math.Round(x.Monto, 2)))];
    }

    private sealed record ResumenSucursal(Guid SucursalId, decimal Monto, int Comprobantes);
}

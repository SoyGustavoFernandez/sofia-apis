using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.POS.Common;

public static class ArqueoCajaCalculator
{
    // Expected drawer cash = opening float + cash received - change given on the session's collected, non-voided sales - cash refunds of returns
    public static async Task<decimal> CalcularEfectivoEsperadoAsync(IApplicationDbContext context, PosSesionCaja sesion, CancellationToken cancellationToken)
    {
        var reembolsosEfectivo = await context.Devoluciones.AsNoTracking()
            .Where(d => d.SesionId == sesion.Id && !d.IsDeleted && d.MetodoReembolso == MetodoPago.Efectivo)
            .SumAsync(d => d.MontoReembolsado ?? 0m, cancellationToken);

        var ventas = await context.Ventas.AsNoTracking()
            .Where(v => v.SesionId == sesion.Id && !v.IsDeleted && v.Estado != EstadoVenta.Anulada && v.Estado != EstadoVenta.Pendiente)
            .Select(v => new { v.Id, v.MontoTotalBruto })
            .ToListAsync(cancellationToken);

        if (ventas.Count == 0)
        {
            return sesion.MontoAperturaEfectivo - reembolsosEfectivo;
        }

        var ventaIds = ventas.Select(v => v.Id).ToList();

        var pagos = await context.VentasPagos.AsNoTracking()
            .Where(p => ventaIds.Contains(p.TransaccionId) && !p.IsDeleted)
            .Select(p => new { p.TransaccionId, p.MetodoPago, p.MontoPagado })
            .ToListAsync(cancellationToken);

        var coberturas = await (
            from r in context.VentasReclamosSeguro.AsNoTracking()
            join d in context.DetallesVenta.AsNoTracking() on r.DetalleVentaId equals d.Id
            where ventaIds.Contains(d.VentaId) && !r.IsDeleted
            select new { d.VentaId, r.MontoCubierto })
            .ToListAsync(cancellationToken);

        var efectivoNeto = ventas.Sum(v =>
        {
            var pagosVenta = pagos.Where(p => p.TransaccionId == v.Id).ToList();
            var montoAPagar = v.MontoTotalBruto - coberturas.Where(c => c.VentaId == v.Id).Sum(c => c.MontoCubierto);
            var vuelto = Math.Max(0m, pagosVenta.Sum(p => p.MontoPagado) - montoAPagar);
            return pagosVenta.Where(p => p.MetodoPago == MetodoPago.Efectivo).Sum(p => p.MontoPagado) - vuelto;
        });

        return sesion.MontoAperturaEfectivo + efectivoNeto - reembolsosEfectivo;
    }
}

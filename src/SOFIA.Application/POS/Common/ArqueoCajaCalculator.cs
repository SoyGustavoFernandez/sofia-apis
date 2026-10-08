using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.POS.Common;

public static class ArqueoCajaCalculator
{
    // Expected drawer cash = opening float + net cash of the sales collected here - net cash of sales of other drawers voided here - cash refunds of returns
    public static async Task<decimal> CalcularEfectivoEsperadoAsync(IApplicationDbContext context, PosSesionCaja sesion, CancellationToken cancellationToken)
    {
        var reembolsosEfectivo = await context.Devoluciones.AsNoTracking()
            .Where(d => d.SesionId == sesion.Id && !d.IsDeleted && d.MetodoReembolso == MetodoPago.Efectivo)
            .SumAsync(d => d.MontoReembolsado ?? 0m, cancellationToken);

        // A sale voided from another drawer keeps counting where it was collected and its net cash leaves the drawer that handed it back
        var ventas = await context.Ventas.AsNoTracking()
            .Where(v => !v.IsDeleted && v.Estado != EstadoVenta.Pendiente
                && ((v.SesionId == sesion.Id && (v.Estado != EstadoVenta.Anulada || (v.SesionAnulacionId != null && v.SesionAnulacionId != sesion.Id)))
                    || (v.SesionAnulacionId == sesion.Id && v.SesionId != sesion.Id)))
            .Select(v => new { v.Id, v.MontoTotalBruto, AnuladaAqui = v.SesionAnulacionId == sesion.Id })
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
            var efectivo = pagosVenta.Where(p => p.MetodoPago == MetodoPago.Efectivo).Sum(p => p.MontoPagado) - vuelto;
            return v.AnuladaAqui ? -efectivo : efectivo;
        });

        return sesion.MontoAperturaEfectivo + efectivoNeto - reembolsosEfectivo;
    }
}

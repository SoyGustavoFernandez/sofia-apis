using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Common;

public static class PagosPorMetodoCalculator
{
    // Same per-sale rule as ArqueoCajaCalculator: change is whatever was handed over above the amount due and always comes out of cash
    public static IReadOnlyDictionary<MetodoPago, decimal> Calcular(IEnumerable<VentaCobrada> ventas, IEnumerable<PagoCobrado> pagos)
    {
        var totales = Enum.GetValues<MetodoPago>().ToDictionary(m => m, _ => 0m);
        var pagosPorVenta = pagos.ToLookup(p => p.VentaId);

        foreach (var venta in ventas)
        {
            var pagosVenta = pagosPorVenta[venta.VentaId].ToList();
            foreach (var pago in pagosVenta)
            {
                totales[pago.Metodo] += pago.Monto;
            }

            totales[MetodoPago.Efectivo] -= Math.Max(0m, pagosVenta.Sum(p => p.Monto) - venta.MontoAPagar);
        }

        return totales;
    }
}

using System.Linq.Expressions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public static class ComprobanteVentaFilters
{
    // Credit notes share the sale's TransaccionId; the sale's own document is its boleta or factura
    public static readonly Expression<Func<SunatComprobanteEmitido, bool>> EsBoletaOFactura =
        c => c.Serie!.TipoComprobante == TipoComprobante.Boleta || c.Serie.TipoComprobante == TipoComprobante.Factura;
}

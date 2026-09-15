using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public static class VentaComprobanteGenerator
{
    public static async Task<ComprobanteEmitidoDto?> GenerateAsync(IApplicationDbContext context, Venta venta, Guid sucursalId, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == "Activa" && !s.IsDeleted, cancellationToken);

        if (serie == null)
        {
            var newSerieResult = SunatSerieFiscal.Create(sucursalId, TipoComprobante.Boleta, "B001", 0, "Activa");
            if (newSerieResult.IsSuccess)
            {
                serie = newSerieResult.Value;
                _ = context.SUNATSeriesFiscales.Add(serie);
            }
        }

        if (serie == null)
        {
            return null;
        }

        var correlativo = serie.CorrelativoActual + 1;
        _ = serie.Update(serie.SucursalId, serie.TipoComprobante, serie.PrefijoSerie, correlativo, serie.EstadoSerie);

        var total = venta.MontoTotalBruto;
        var comprobanteResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            "1", "00000000", "CLIENTE EVENTUAL",
            total * 0.82m, 0, total * 0.18m, total,
            "HASH_SIMULATED_" + Guid.NewGuid().ToString("N")[..8],
            "Aceptado",
            $"/comprobantes/XML_{correlativo}.xml",
            $"/comprobantes/CDR_{correlativo}.xml",
            $"https://sunat.gob.pe/verificar/{serie.PrefijoSerie}-{correlativo}");

        if (!comprobanteResult.IsSuccess)
        {
            return null;
        }

        _ = context.SUNATComprobantesEmitidos.Add(comprobanteResult.Value);
        return new ComprobanteEmitidoDto(
            serie.TipoComprobante.ToString(),
            $"{serie.PrefijoSerie}-{correlativo:D8}",
            "Aceptado",
            comprobanteResult.Value.UrlPublicaVerificacion,
            comprobanteResult.Value.RutaArchivoXml,
            comprobanteResult.Value.RutaArchivoCdr);
    }
}

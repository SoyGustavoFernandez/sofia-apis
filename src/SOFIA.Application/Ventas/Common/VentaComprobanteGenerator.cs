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

        int correlativo;
        if (serie == null)
        {
            // Brand-new series, no concurrent row to race against — start it directly at correlativo 1.
            var newSerieResult = SunatSerieFiscal.Create(sucursalId, TipoComprobante.Boleta, "B001", 1, "Activa");
            if (!newSerieResult.IsSuccess)
            {
                return null;
            }

            serie = newSerieResult.Value;
            _ = context.SUNATSeriesFiscales.Add(serie);
            correlativo = serie.CorrelativoActual;
        }
        else
        {
            correlativo = await context.IncrementarCorrelativoSunatAsync(serie.Id, cancellationToken);
        }

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

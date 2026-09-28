using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public static class VentaComprobanteGenerator
{
    // The series is resolved before the correlative UPDATE, which executes immediately and is kept even when the sale fails
    public static async Task<Result<ComprobanteEmitidoDto>> EmitirBoletaAsync(IApplicationDbContext context, Venta venta, Guid sucursalId, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == SunatSerieFiscal.EstadoActiva && !s.IsDeleted, cancellationToken);

        return serie == null
            ? Result.Failure<ComprobanteEmitidoDto>(Error.Validation("Venta.SerieBoleta.NoConfigurada", "The branch has no active boleta series."))
            : await GenerateAsync(context, venta, serie, cancellationToken);
    }

    private static async Task<Result<ComprobanteEmitidoDto>> GenerateAsync(IApplicationDbContext context, Venta venta, SunatSerieFiscal serie, CancellationToken cancellationToken)
    {
        var correlativo = await context.IncrementarCorrelativoSunatAsync(serie.Id, cancellationToken);

        var total = venta.MontoTotalBruto;
        var (gravado, igv) = DesglosarIgv(total);
        var comprobanteResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            "1", "00000000", "CLIENTE EVENTUAL",
            gravado, 0, igv, total,
            "HASH_SIMULATED_" + Guid.NewGuid().ToString("N")[..8],
            "Aceptado",
            $"/comprobantes/XML_{correlativo}.xml",
            $"/comprobantes/CDR_{correlativo}.xml",
            $"https://sunat.gob.pe/verificar/{serie.PrefijoSerie}-{correlativo}");

        if (!comprobanteResult.IsSuccess)
        {
            return Result.Failure<ComprobanteEmitidoDto>(comprobanteResult.Error);
        }

        _ = context.SUNATComprobantesEmitidos.Add(comprobanteResult.Value);
        return Result.Success(new ComprobanteEmitidoDto(
            serie.TipoComprobante.ToString(),
            $"{serie.PrefijoSerie}-{correlativo:D8}",
            "Aceptado",
            comprobanteResult.Value.UrlPublicaVerificacion,
            comprobanteResult.Value.RutaArchivoXml,
            comprobanteResult.Value.RutaArchivoCdr));
    }

    // Prices include IGV: the taxable base is total / 1.18 and IGV is the remainder, so both always add up to the total
    public static (decimal Gravado, decimal Igv) DesglosarIgv(decimal total)
    {
        var gravado = Math.Round(total / 1.18m, 2, MidpointRounding.AwayFromZero);
        return (gravado, total - gravado);
    }
}

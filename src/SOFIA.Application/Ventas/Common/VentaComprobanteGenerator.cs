using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Common;

public static class VentaComprobanteGenerator
{
    public const string NumeroClienteEventual = "00000000";
    public const string NombreClienteEventual = "CLIENTE EVENTUAL";

    // Issuer and series are resolved before the correlative UPDATE, which executes immediately and is kept even when the sale fails
    public static async Task<Result<ComprobanteEmitidoDto>> EmitirBoletaAsync(IApplicationDbContext context, Venta venta, Guid sucursalId, CancellationToken cancellationToken)
    {
        var emisor = await GetEmisorAsync(context, sucursalId, cancellationToken);
        if (emisor?.Ruc is null)
        {
            return Result.Failure<ComprobanteEmitidoDto>(Error.Validation("Venta.Empresa.SinRuc", "The company has no RUC, so it cannot issue receipts."));
        }

        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == SunatSerieFiscal.EstadoActiva && !s.IsDeleted, cancellationToken);

        return serie == null
            ? Result.Failure<ComprobanteEmitidoDto>(Error.Validation("Venta.SerieBoleta.NoConfigurada", "The branch has no active boleta series."))
            : await GenerateAsync(context, venta, serie, emisor, cancellationToken);
    }

    public static async Task<EmisorComprobante?> GetEmisorAsync(IApplicationDbContext context, Guid sucursalId, CancellationToken cancellationToken)
    {
        var emisor = await context.Sucursales
            .Where(s => s.Id == sucursalId)
            .Join(context.Empresas, s => s.EmpresaId, e => (Guid?)e.Id, (s, e) => new { e.Nombre, e.RUC, s.Direccion_Fisica })
            .FirstOrDefaultAsync(cancellationToken);

        return emisor == null ? null : new EmisorComprobante(emisor.Nombre, emisor.RUC?.Value, emisor.Direccion_Fisica);
    }

    private static async Task<Result<ComprobanteEmitidoDto>> GenerateAsync(IApplicationDbContext context, Venta venta, SunatSerieFiscal serie, EmisorComprobante emisor, CancellationToken cancellationToken)
    {
        var (TipoDocumento, Numero, Nombre) = await ResolveClienteAsync(context, venta.ClienteId, cancellationToken);
        var correlativo = await context.IncrementarCorrelativoSunatAsync(serie.Id, cancellationToken);

        var total = venta.MontoTotalBruto;
        var (gravado, igv) = DesglosarIgv(total);

        // Not sent to SUNAT yet: hash, XML, CDR and verification URL stay empty until an OSE/PSE is integrated
        var comprobanteResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            TipoDocumento, Numero, Nombre,
            gravado, 0, igv, total,
            null,
            EstadoComprobanteSunat.NoEnviado,
            null,
            null,
            null);

        if (!comprobanteResult.IsSuccess)
        {
            return Result.Failure<ComprobanteEmitidoDto>(comprobanteResult.Error);
        }

        _ = context.SUNATComprobantesEmitidos.Add(comprobanteResult.Value);
        return Result.Success(new ComprobanteEmitidoDto(
            serie.TipoComprobante.ToString(),
            $"{serie.PrefijoSerie}-{correlativo:D8}",
            EstadoComprobanteSunat.NoEnviado,
            null,
            null,
            null,
            emisor.RazonSocial,
            emisor.Ruc,
            emisor.Direccion,
            TipoDocumento,
            Numero,
            Nombre));
    }

    // Patients registered before the document type existed are billed as "varios" with their own number and name
    private static async Task<(string TipoDocumento, string Numero, string Nombre)> ResolveClienteAsync(IApplicationDbContext context, Guid? clienteId, CancellationToken cancellationToken)
    {
        var paciente = clienteId is null
            ? null
            : await context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == clienteId, cancellationToken);

        return paciente is null || paciente.DocIdentidadGub.Length > 20
            ? (TipoDocumentoIdentidad.Varios, NumeroClienteEventual, NombreClienteEventual)
            : (paciente.TipoDocumento ?? TipoDocumentoIdentidad.Varios, paciente.DocIdentidadGub, paciente.NombreApellidos);
    }

    // Prices include IGV: the taxable base is total / 1.18 and IGV is the remainder, so both always add up to the total
    public static (decimal Gravado, decimal Igv) DesglosarIgv(decimal total)
    {
        var gravado = Math.Round(total / 1.18m, 2, MidpointRounding.AwayFromZero);
        return (gravado, total - gravado);
    }
}

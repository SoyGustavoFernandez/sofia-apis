using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class SunatComprobanteEmitido : BaseEntity
{
    private SunatComprobanteEmitido() { }

    public Guid TransaccionId { get; private set; }
    public Guid SerieId { get; private set; }
    public int NumeroCorrelativo { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public string TipoDocIdentidadCliente { get; private set; } = string.Empty;
    public string NumeroIdentidadCliente { get; private set; } = string.Empty;
    public string RazonSocialCliente { get; private set; } = string.Empty;
    public decimal MontoGravadoIgv { get; private set; }
    public decimal MontoExonerado { get; private set; }
    public decimal MontoTotalIgv { get; private set; }
    public decimal MontoTotalVenta { get; private set; }
    public string? HashFirmaDigital { get; private set; }
    public string EstadoAceptacion { get; private set; } = string.Empty;
    public string? RutaArchivoXml { get; private set; }
    public string? RutaArchivoCdr { get; private set; }
    public string? UrlPublicaVerificacion { get; private set; }

    // Navigation Properties
    public Venta? Transaccion { get; }
    public SunatSerieFiscal? Serie { get; }

    public static Result<SunatComprobanteEmitido> Create(
        Guid transaccionId,
        Guid serieId,
        int numeroCorrelativo,
        string tipoDocIdentidadCliente,
        string numeroIdentidadCliente,
        string razonSocialCliente,
        decimal montoGravadoIgv,
        decimal montoExonerado,
        decimal montoTotalIgv,
        decimal montoTotalVenta,
        string? hashFirmaDigital,
        string estadoAceptacion,
        string? rutaArchivoXml,
        string? rutaArchivoCdr,
        string? urlPublicaVerificacion,
        DateTime? fechaEmision = null)
    {
        var idError = ValidateIds(transaccionId, serieId, numeroCorrelativo);
        if (idError is not null)
        {
            return idError;
        }

        var clientError = ValidateClientData(tipoDocIdentidadCliente, numeroIdentidadCliente, razonSocialCliente);
        if (clientError is not null)
        {
            return clientError;
        }

        var montoError = ValidateMontos(montoGravadoIgv, montoExonerado, montoTotalIgv, montoTotalVenta);
        if (montoError is not null)
        {
            return montoError;
        }

        var fieldError = ValidateOptionalFields(hashFirmaDigital, estadoAceptacion, rutaArchivoXml, rutaArchivoCdr, urlPublicaVerificacion);
        if (fieldError is not null)
        {
            return fieldError;
        }

        return Result.Success(new SunatComprobanteEmitido
        {
            TransaccionId = transaccionId,
            SerieId = serieId,
            NumeroCorrelativo = numeroCorrelativo,
            FechaEmision = fechaEmision ?? DateTime.UtcNow,
            TipoDocIdentidadCliente = tipoDocIdentidadCliente,
            NumeroIdentidadCliente = numeroIdentidadCliente,
            RazonSocialCliente = razonSocialCliente,
            MontoGravadoIgv = montoGravadoIgv,
            MontoExonerado = montoExonerado,
            MontoTotalIgv = montoTotalIgv,
            MontoTotalVenta = montoTotalVenta,
            HashFirmaDigital = hashFirmaDigital,
            EstadoAceptacion = estadoAceptacion,
            RutaArchivoXml = rutaArchivoXml,
            RutaArchivoCdr = rutaArchivoCdr,
            UrlPublicaVerificacion = urlPublicaVerificacion
        });
    }

    public Result Update(
        Guid transaccionId,
        Guid serieId,
        int numeroCorrelativo,
        string tipoDocIdentidadCliente,
        string numeroIdentidadCliente,
        string razonSocialCliente,
        decimal montoGravadoIgv,
        decimal montoExonerado,
        decimal montoTotalIgv,
        decimal montoTotalVenta,
        string? hashFirmaDigital,
        string estadoAceptacion,
        string? rutaArchivoXml,
        string? rutaArchivoCdr,
        string? urlPublicaVerificacion)
    {
        var idError = ValidateIds(transaccionId, serieId, numeroCorrelativo);
        if (idError is not null)
        {
            return idError;
        }

        var clientError = ValidateClientData(tipoDocIdentidadCliente, numeroIdentidadCliente, razonSocialCliente);
        if (clientError is not null)
        {
            return clientError;
        }

        var montoError = ValidateMontos(montoGravadoIgv, montoExonerado, montoTotalIgv, montoTotalVenta);
        if (montoError is not null)
        {
            return montoError;
        }

        var fieldError = ValidateOptionalFields(hashFirmaDigital, estadoAceptacion, rutaArchivoXml, rutaArchivoCdr, urlPublicaVerificacion);
        if (fieldError is not null)
        {
            return fieldError;
        }

        TransaccionId = transaccionId;
        SerieId = serieId;
        NumeroCorrelativo = numeroCorrelativo;
        TipoDocIdentidadCliente = tipoDocIdentidadCliente;
        NumeroIdentidadCliente = numeroIdentidadCliente;
        RazonSocialCliente = razonSocialCliente;
        MontoGravadoIgv = montoGravadoIgv;
        MontoExonerado = montoExonerado;
        MontoTotalIgv = montoTotalIgv;
        MontoTotalVenta = montoTotalVenta;
        HashFirmaDigital = hashFirmaDigital;
        EstadoAceptacion = estadoAceptacion;
        RutaArchivoXml = rutaArchivoXml;
        RutaArchivoCdr = rutaArchivoCdr;
        UrlPublicaVerificacion = urlPublicaVerificacion;

        return Result.Success();
    }

    private static Result<SunatComprobanteEmitido>? ValidateIds(Guid transaccionId, Guid serieId, int numeroCorrelativo)
    {
        if (transaccionId == Guid.Empty)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.TransaccionId", "Transaccion ID is required."));
        }

        if (serieId == Guid.Empty)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.SerieId", "Serie ID is required."));
        }

        if (numeroCorrelativo <= 0)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.NumeroCorrelativo", "Numero Correlativo must be greater than zero."));
        }

        return null;
    }

    private static Result<SunatComprobanteEmitido>? ValidateClientData(
        string tipoDocIdentidadCliente, string numeroIdentidadCliente, string razonSocialCliente)
    {
        if (string.IsNullOrWhiteSpace(tipoDocIdentidadCliente))
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente is required."));
        }

        if (tipoDocIdentidadCliente.Length > 1)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente must not exceed 1 character."));
        }

        if (string.IsNullOrWhiteSpace(numeroIdentidadCliente))
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente is required."));
        }

        if (numeroIdentidadCliente.Length > 20)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente must not exceed 20 characters."));
        }

        if (string.IsNullOrWhiteSpace(razonSocialCliente))
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente is required."));
        }

        if (razonSocialCliente.Length > 200)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente must not exceed 200 characters."));
        }

        return null;
    }

    private static Result<SunatComprobanteEmitido>? ValidateMontos(
        decimal montoGravadoIgv, decimal montoExonerado, decimal montoTotalIgv, decimal montoTotalVenta)
    {
        if (montoGravadoIgv < 0)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.MontoGravadoIgv", "Monto Gravado IGV must be greater than or equal to zero."));
        }

        if (montoExonerado < 0)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.MontoExonerado", "Monto Exonerado must be greater than or equal to zero."));
        }

        if (montoTotalIgv < 0)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.MontoTotalIgv", "Monto Total IGV must be greater than or equal to zero."));
        }

        if (montoTotalVenta < 0)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.MontoTotalVenta", "Monto Total Venta must be greater than or equal to zero."));
        }

        return null;
    }

    private static Result<SunatComprobanteEmitido>? ValidateOptionalFields(
        string? hashFirmaDigital, string estadoAceptacion,
        string? rutaArchivoXml, string? rutaArchivoCdr, string? urlPublicaVerificacion)
    {
        if (hashFirmaDigital != null && hashFirmaDigital.Length > 255)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.HashFirmaDigital", "Hash Firma Digital must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(estadoAceptacion))
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.EstadoAceptacion", "Estado Aceptación is required."));
        }

        if (estadoAceptacion.Length > 20)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.EstadoAceptacion", "Estado Aceptación must not exceed 20 characters."));
        }

        if (rutaArchivoXml != null && rutaArchivoXml.Length > 500)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.RutaArchivoXml", "Ruta Archivo XML must not exceed 500 characters."));
        }

        if (rutaArchivoCdr != null && rutaArchivoCdr.Length > 500)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.RutaArchivoCdr", "Ruta Archivo CDR must not exceed 500 characters."));
        }

        if (urlPublicaVerificacion != null && urlPublicaVerificacion.Length > 500)
        {
            return Result.Failure<SunatComprobanteEmitido>(Error.Validation("SunatComprobanteEmitido.UrlPublicaVerificacion", "URL Publica Verificación must not exceed 500 characters."));
        }

        return null;
    }
}

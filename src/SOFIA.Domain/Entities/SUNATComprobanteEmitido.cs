using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class SUNATComprobanteEmitido : BaseEntity
{
    private SUNATComprobanteEmitido() { }

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
    public Venta? Transaccion { get; private set; }
    public SUNATSerieFiscal? Serie { get; private set; }

    public static Result<SUNATComprobanteEmitido> Create(
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
        if (transaccionId == Guid.Empty)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.TransaccionId", "Transaccion ID is required."));
        }

        if (serieId == Guid.Empty)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.SerieId", "Serie ID is required."));
        }

        if (numeroCorrelativo <= 0)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.NumeroCorrelativo", "Numero Correlativo must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(tipoDocIdentidadCliente))
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente is required."));
        }

        if (tipoDocIdentidadCliente.Length > 1)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente must not exceed 1 character."));
        }

        if (string.IsNullOrWhiteSpace(numeroIdentidadCliente))
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente is required."));
        }

        if (numeroIdentidadCliente.Length > 20)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente must not exceed 20 characters."));
        }

        if (string.IsNullOrWhiteSpace(razonSocialCliente))
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente is required."));
        }

        if (razonSocialCliente.Length > 200)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente must not exceed 200 characters."));
        }

        if (montoGravadoIgv < 0)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.MontoGravadoIgv", "Monto Gravado IGV must be greater than or equal to zero."));
        }

        if (montoExonerado < 0)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.MontoExonerado", "Monto Exonerado must be greater than or equal to zero."));
        }

        if (montoTotalIgv < 0)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.MontoTotalIgv", "Monto Total IGV must be greater than or equal to zero."));
        }

        if (montoTotalVenta < 0)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.MontoTotalVenta", "Monto Total Venta must be greater than or equal to zero."));
        }

        if (hashFirmaDigital != null && hashFirmaDigital.Length > 255)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.HashFirmaDigital", "Hash Firma Digital must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(estadoAceptacion))
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.EstadoAceptacion", "Estado Aceptación is required."));
        }

        if (estadoAceptacion.Length > 20)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.EstadoAceptacion", "Estado Aceptación must not exceed 20 characters."));
        }

        if (rutaArchivoXml != null && rutaArchivoXml.Length > 500)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.RutaArchivoXml", "Ruta Archivo XML must not exceed 500 characters."));
        }

        if (rutaArchivoCdr != null && rutaArchivoCdr.Length > 500)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.RutaArchivoCdr", "Ruta Archivo CDR must not exceed 500 characters."));
        }

        if (urlPublicaVerificacion != null && urlPublicaVerificacion.Length > 500)
        {
            return Result.Failure<SUNATComprobanteEmitido>(Error.Validation("SUNATComprobanteEmitido.UrlPublicaVerificacion", "URL Publica Verificación must not exceed 500 characters."));
        }

        return Result.Success(new SUNATComprobanteEmitido
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
        if (transaccionId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.TransaccionId", "Transaccion ID is required."));
        }

        if (serieId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.SerieId", "Serie ID is required."));
        }

        if (numeroCorrelativo <= 0)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.NumeroCorrelativo", "Numero Correlativo must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(tipoDocIdentidadCliente))
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente is required."));
        }

        if (tipoDocIdentidadCliente.Length > 1)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.TipoDocIdentidadCliente", "Tipo Doc Identidad Cliente must not exceed 1 character."));
        }

        if (string.IsNullOrWhiteSpace(numeroIdentidadCliente))
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente is required."));
        }

        if (numeroIdentidadCliente.Length > 20)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.NumeroIdentidadCliente", "Numero Identidad Cliente must not exceed 20 characters."));
        }

        if (string.IsNullOrWhiteSpace(razonSocialCliente))
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente is required."));
        }

        if (razonSocialCliente.Length > 200)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.RazonSocialCliente", "Razón Social Cliente must not exceed 200 characters."));
        }

        if (montoGravadoIgv < 0)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.MontoGravadoIgv", "Monto Gravado IGV must be greater than or equal to zero."));
        }

        if (montoExonerado < 0)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.MontoExonerado", "Monto Exonerado must be greater than or equal to zero."));
        }

        if (montoTotalIgv < 0)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.MontoTotalIgv", "Monto Total IGV must be greater than or equal to zero."));
        }

        if (montoTotalVenta < 0)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.MontoTotalVenta", "Monto Total Venta must be greater than or equal to zero."));
        }

        if (hashFirmaDigital != null && hashFirmaDigital.Length > 255)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.HashFirmaDigital", "Hash Firma Digital must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(estadoAceptacion))
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.EstadoAceptacion", "Estado Aceptación is required."));
        }

        if (estadoAceptacion.Length > 20)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.EstadoAceptacion", "Estado Aceptación must not exceed 20 characters."));
        }

        if (rutaArchivoXml != null && rutaArchivoXml.Length > 500)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.RutaArchivoXml", "Ruta Archivo XML must not exceed 500 characters."));
        }

        if (rutaArchivoCdr != null && rutaArchivoCdr.Length > 500)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.RutaArchivoCdr", "Ruta Archivo CDR must not exceed 500 characters."));
        }

        if (urlPublicaVerificacion != null && urlPublicaVerificacion.Length > 500)
        {
            return Result.Failure(Error.Validation("SUNATComprobanteEmitido.UrlPublicaVerificacion", "URL Publica Verificación must not exceed 500 characters."));
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
}

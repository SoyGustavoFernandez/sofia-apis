namespace SOFIA.Application.Ventas.Common;

// Issued locally and not sent to SUNAT yet; hash/URL/XML/CDR stay null until a certified OSE/PSE is integrated
public record ComprobanteEmitidoDto(
    string Tipo,
    string Numero,
    string EstadoAceptacion,
    string? UrlVerificacion,
    string? UrlXml,
    string? UrlCdr,
    string? EmisorRazonSocial,
    string? EmisorRuc,
    string? SucursalDireccion,
    string ClienteTipoDocumento,
    string ClienteNumeroDocumento,
    string ClienteNombre);

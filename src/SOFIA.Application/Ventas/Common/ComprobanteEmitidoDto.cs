namespace SOFIA.Application.Ventas.Common;

// SUNAT integration is simulated; hash/URL/CDR are placeholders until a certified OSE/PSE is integrated.
public record ComprobanteEmitidoDto(string Tipo, string Numero, string EstadoAceptacion, string? UrlVerificacion, string? UrlXml, string? UrlCdr);

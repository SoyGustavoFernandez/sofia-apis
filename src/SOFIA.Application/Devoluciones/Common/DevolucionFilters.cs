using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Devoluciones.Common;

public static class DevolucionFilters
{
    private const char NumeroSeparator = '-';

    // Accepts the printed number, only its series prefix or only its correlative, so a cashier can type what the receipt shows
    public static (string? Prefijo, int? Correlativo) ParseNumeroNotaCredito(string? numero)
    {
        var texto = numero?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            return (null, null);
        }

        var separador = texto.IndexOf(NumeroSeparator, StringComparison.Ordinal);
        if (separador >= 0)
        {
            var prefijo = texto[..separador].Trim();
            var tail = texto[(separador + 1)..].Trim();
            return (prefijo.Length > 0 ? prefijo : null, int.TryParse(tail, out var parcial) ? parcial : null);
        }

        return int.TryParse(texto, out var correlativo) ? (null, correlativo) : (texto, null);
    }

    public static IQueryable<DevolucionCabecera> WhereNumeroNotaCredito(this IQueryable<DevolucionCabecera> query, IApplicationDbContext context, string? numero)
    {
        var (prefijo, correlativo) = ParseNumeroNotaCredito(numero);
        if (prefijo is null && correlativo is null)
        {
            return query;
        }

        // Returns without a credit note never match, whatever the typed number
        return query.Where(d => context.SUNATComprobantesEmitidos.Any(c =>
            c.Id == d.ComprobanteNcId
            && (prefijo == null || c.Serie!.PrefijoSerie.Contains(prefijo))
            && (correlativo == null || c.NumeroCorrelativo == correlativo)));
    }
}

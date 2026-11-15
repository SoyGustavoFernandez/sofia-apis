using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

/// <summary>The listing fields of a return plus its lines, flat like the sale detail.</summary>
public record DevolucionConDetalleDto(
    Guid Id,
    Guid ComprobanteOrigenId,
    Guid EmpleadoAutorizaId,
    DateTime FechaDevolucion,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo,
    Guid? VentaId,
    string? CodigoVenta,
    string? NumeroNotaCredito,
    string ClienteNombre,
    string EmpleadoAutorizaNombre,
    string? SucursalNombre,
    decimal? MontoNotaCredito,
    decimal? MontoReembolsado,
    string? MetodoReembolso,
    IReadOnlyList<DevolucionLineaDto> Lineas
)
{
    public static DevolucionConDetalleDto From(DevolucionResumenDto r, IReadOnlyList<DevolucionLineaDto> lineas) => new(
        r.Id,
        r.ComprobanteOrigenId,
        r.EmpleadoAutorizaId,
        r.FechaDevolucion,
        r.MotivoSunatCatalogo,
        r.SustentoDescriptivo,
        r.VentaId,
        r.CodigoVenta,
        r.NumeroNotaCredito,
        r.ClienteNombre,
        r.EmpleadoAutorizaNombre,
        r.SucursalNombre,
        r.MontoNotaCredito,
        r.MontoReembolsado,
        r.MetodoReembolso,
        lineas);
}

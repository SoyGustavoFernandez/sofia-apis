namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

/// <summary>VentaId is the sale whose receipt the return credits; MontoNotaCredito is the gross amount credited and MontoReembolsado what left the drawer.</summary>
public record DevolucionResumenDto(
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
    string? MetodoReembolso
);

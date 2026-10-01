namespace SOFIA.Application.Ventas.Queries.GetLotesDisponiblesVenta;

public record LoteDisponibleVentaDto(
    Guid Id,
    Guid LoteId,
    string NumeroLote,
    Guid ProductoId,
    string ProductoNombre,
    DateTimeOffset FechaCaducidad,
    decimal CantidadFisica,
    decimal? PrecioVentaBase);

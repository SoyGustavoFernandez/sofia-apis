namespace SOFIA.Application.Inventarios;

public record StockPorSucursalDto(
    Guid Id,
    Guid SucursalId,
    string SucursalNombre,
    Guid LoteId,
    string NumeroLote,
    Guid ProductoId,
    string ProductoNombre,
    DateTimeOffset FechaCaducidad,
    decimal CantidadFisica,
    decimal? PrecioVentaBase);

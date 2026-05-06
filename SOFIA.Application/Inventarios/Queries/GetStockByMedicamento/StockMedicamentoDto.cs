namespace SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;

public record StockMedicamentoDto(
    Guid MedicamentoId,
    string NombreMedicamento,
    decimal StockTotal,
    List<StockSucursalDto> DesgloseSucursales,
    List<StockLoteDto> DesgloseLotes);

public record StockSucursalDto(
    Guid SucursalId,
    string NombreSucursal,
    decimal Cantidad);

public record StockLoteDto(
    Guid LoteId,
    string NumeroLote,
    DateTimeOffset FechaCaducidad,
    decimal Cantidad);

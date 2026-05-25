using SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;

namespace SOFIA.Application.Medicamentos;

public record MedicamentoDto(
    Guid Id,
    string CodigoNacional,
    string NombreComercial,
    Guid LaboratorioId,
    string LaboratorioNombre,
    Guid UnidadBaseId,
    string UnidadBaseNombre,
    Domain.Enums.CondicionVenta CondicionVenta,
    decimal? StockTotal = null,
    List<StockSucursalDto>? StockPorSucursal = null);

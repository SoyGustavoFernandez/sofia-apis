using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales;

public record SerieFiscalDto(
    Guid Id,
    Guid SucursalId,
    string SucursalNombre,
    TipoComprobante TipoComprobante,
    string PrefijoSerie,
    int CorrelativoActual,
    string EstadoSerie,
    bool TieneComprobantes,
    DateTimeOffset CreatedAt);

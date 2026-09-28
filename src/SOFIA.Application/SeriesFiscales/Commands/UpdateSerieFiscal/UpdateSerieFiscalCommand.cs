using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;

public record UpdateSerieFiscalCommand(
    Guid Id,
    Guid SucursalId,
    TipoComprobante TipoComprobante,
    string PrefijoSerie,
    int CorrelativoActual,
    string EstadoSerie) : ICommand;

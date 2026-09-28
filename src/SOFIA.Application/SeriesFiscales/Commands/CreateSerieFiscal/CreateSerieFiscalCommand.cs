using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;

public record CreateSerieFiscalCommand(
    Guid SucursalId,
    TipoComprobante TipoComprobante,
    string PrefijoSerie,
    int CorrelativoActual,
    string EstadoSerie) : ICommand<Guid>;

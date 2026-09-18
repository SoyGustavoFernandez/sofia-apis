using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;

public record UpdatePresentacionVentaCommand(
    Guid Id,
    Guid UnidadVentaId,
    decimal PrecioVenta) : ICommand;

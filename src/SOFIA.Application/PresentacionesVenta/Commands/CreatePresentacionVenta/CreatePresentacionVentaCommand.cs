using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;

public record CreatePresentacionVentaCommand(
    Guid ProductoId,
    Guid UnidadVentaId,
    decimal PrecioVenta) : ICommand<Guid>;

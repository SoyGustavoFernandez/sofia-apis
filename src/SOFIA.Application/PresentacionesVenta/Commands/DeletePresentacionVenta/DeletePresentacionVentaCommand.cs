using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.PresentacionesVenta.Commands.DeletePresentacionVenta;

public record DeletePresentacionVentaCommand(Guid Id) : ICommand;

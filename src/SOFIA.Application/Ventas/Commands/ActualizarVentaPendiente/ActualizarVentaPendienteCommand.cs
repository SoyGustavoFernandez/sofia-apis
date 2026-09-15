using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;

namespace SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;

public record ActualizarVentaPendienteCommand(
    Guid VentaId,
    List<CreateVentaDetailDto> Detalles,
    Guid? ClienteId = null) : ICommand;

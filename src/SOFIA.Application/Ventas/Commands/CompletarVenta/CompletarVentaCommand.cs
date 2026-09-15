using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;

namespace SOFIA.Application.Ventas.Commands.CompletarVenta;

public record CompletarVentaCommand(
    Guid VentaId,
    List<CreateVentaPagoDto> Pagos,
    List<CreateVentaDetailDto>? Detalles = null,
    Guid? ClienteId = null,
    Guid? AseguradoraId = null,
    decimal? MontoCubiertoSeguro = null) : ICommand<VentaCreadaDto>;

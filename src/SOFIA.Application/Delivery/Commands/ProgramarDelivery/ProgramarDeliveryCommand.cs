using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.ProgramarDelivery;

public record ProgramarDeliveryCommand(Guid VentaId, string PlataformaServicio, string? CodigoRastreo, string DireccionEntrega, string? RepartidorNombre, string? EvidenciaFotograficaUrl) : ICommand<Guid>;

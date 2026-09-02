using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Events;

public record VentaCompletadaEvent(Guid VentaId, Guid SucursalId) : IDomainEvent, INotification;

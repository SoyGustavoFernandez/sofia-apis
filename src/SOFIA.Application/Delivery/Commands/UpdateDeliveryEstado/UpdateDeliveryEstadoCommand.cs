using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.UpdateDeliveryEstado;

public record UpdateDeliveryEstadoCommand(Guid Id, Domain.Enums.EstadoDespacho NuevoEstado) : ICommand<Guid>;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.UpdateDeliveryEstado;

public class UpdateDeliveryEstadoCommandValidator : AbstractValidator<UpdateDeliveryEstadoCommand>
{
    public UpdateDeliveryEstadoCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.NuevoEstado).IsInEnum();
    }
}

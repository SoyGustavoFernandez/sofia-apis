using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.UpdateDeliveryEstado;

public record UpdateDeliveryEstadoCommand(Guid Id, Domain.Enums.EstadoDespacho NuevoEstado) : IRequest<Result<Guid>>;

public class UpdateDeliveryEstadoCommandValidator : AbstractValidator<UpdateDeliveryEstadoCommand>
{
    public UpdateDeliveryEstadoCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.NuevoEstado).IsInEnum();
    }
}

public class UpdateDeliveryEstadoCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdateDeliveryEstadoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateDeliveryEstadoCommand request, CancellationToken cancellationToken)
    {
        var delivery = await dbContext.DespachosDelivery.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (delivery == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Delivery", "Despacho no encontrado"));
        }

        var result = delivery.Update(delivery.PlataformaServicio, delivery.CodigoRastreo, request.NuevoEstado, delivery.DireccionEntrega, delivery.RepartidorNombre, delivery.EvidenciaFotograficaUrl);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(delivery.Id);
    }
}

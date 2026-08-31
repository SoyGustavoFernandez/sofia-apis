using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;

public class UpdateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateJerarquiaUoMCommand, Result>
{
    public async Task<Result> Handle(UpdateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("JerarquiaUoM.NotFound", $"JerarquÃ­a with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

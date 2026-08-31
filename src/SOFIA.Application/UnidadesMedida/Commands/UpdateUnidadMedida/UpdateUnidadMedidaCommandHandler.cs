using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;

public class UpdateUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateUnidadMedidaCommand, Result>
{
    public async Task<Result> Handle(UpdateUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.UnidadesMedida
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.Codigo, request.Descripcion);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

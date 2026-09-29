using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var duplicado = await context.UnidadesMedida
            .AnyAsync(u => u.Id != entity.Id && u.Codigo == request.Codigo && !u.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("UnidadMedida.Codigo.Duplicado", "Another unit of measure already uses this code."), 409);
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

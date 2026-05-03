using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;

public record DeleteUnidadMedidaCommand(Guid Id) : IRequest<Result>;

public class DeleteUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteUnidadMedidaCommand, Result>
{
    public async Task<Result> Handle(DeleteUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.UnidadesMedida
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.Id} was not found."), 404);
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

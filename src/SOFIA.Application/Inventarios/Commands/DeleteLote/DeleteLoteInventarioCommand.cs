using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.DeleteLote;

public record DeleteLoteInventarioCommand(Guid Id) : ICommand;

public class DeleteLoteInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteLoteInventarioCommand, Result>
{
    public async Task<Result> Handle(DeleteLoteInventarioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.LotesInventario
            .FindAsync([request.Id], cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        // The ApplicationDbContext handles the soft delete logic in SaveChangesAsync
        _ = context.LotesInventario.Remove(entity);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

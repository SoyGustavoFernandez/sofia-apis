using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.DeleteDigemidProducto;

public record DeleteDigemidProductoCommand(Guid Id) : ICommand;

public class DeleteDigemidProductoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteDigemidProductoCommand, Result>
{
    public async Task<Result> Handle(DeleteDigemidProductoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.DigemidCatalogoProductos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("DigemidCatalogo.NotFound", $"DigemidCatalogoProducto with ID {request.Id} was not found."), 404);
        }

        _ = context.DigemidCatalogoProductos.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

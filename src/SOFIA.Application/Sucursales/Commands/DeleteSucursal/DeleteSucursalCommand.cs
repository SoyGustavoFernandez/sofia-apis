using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.DeleteSucursal;

public record DeleteSucursalCommand(Guid Id) : IRequest<Result>;

public class DeleteSucursalCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteSucursalCommand, Result>
{
    public async Task<Result> Handle(DeleteSucursalCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Sucursales
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", $"Sucursal with ID {request.Id} was not found."), 404);
        }

        _ = context.Sucursales.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Commands.DeleteEmpleado;

public record DeleteEmpleadoCommand(Guid Id) : IRequest<Result>;

public class DeleteEmpleadoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteEmpleadoCommand, Result>
{
    public async Task<Result> Handle(DeleteEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Empleados
            .Include(e => e.Sucursal_Gerenciada)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Empleado.NotFound", $"Empleado with ID {request.Id} was not found."), 404);
        }

        if (entity.Sucursal_Gerenciada != null)
        {
            return Result.Failure(Error.Validation("Empleado.DeleteError",
                $"Cannot delete employee because they are the Manager of branch: {entity.Sucursal_Gerenciada.Nombre}. Please assign a new manager first."));
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

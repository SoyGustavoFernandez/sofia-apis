using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.DeleteRol;

public record DeleteRolCommand(Guid Id) : ICommand;

public class DeleteRolCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteRolCommand, Result>
{
    public async Task<Result> Handle(DeleteRolCommand request, CancellationToken cancellationToken)
    {
        var rol = await context.Roles
            .Include(r => r.Cuentas)
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (rol is null)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol especificado no existe."));
        }

        if (rol.Cuentas.Any(c => !c.IsDeleted))
        {
            return Result.Failure(Error.Conflict("Rol.InUse", "No se puede eliminar un rol que tiene usuarios activos vinculados."));
        }

        _ = context.Roles.Remove(rol);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

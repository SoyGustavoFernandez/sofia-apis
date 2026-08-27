using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.UpdateRol;

public record UpdateRolCommand(Guid Id, string? Descripcion, int NivelJerarquia) : ICommand;

public class UpdateRolCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateRolCommand, Result>
{
    public async Task<Result> Handle(UpdateRolCommand request, CancellationToken cancellationToken)
    {
        var rol = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (rol is null)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol especificado no existe."));
        }

        rol.Update(request.Descripcion, request.NivelJerarquia);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

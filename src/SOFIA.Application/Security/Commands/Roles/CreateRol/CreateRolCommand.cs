using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.CreateRol;

public record CreateRolCommand(string NombreRol, string? Descripcion, int NivelJerarquia) : ICommand<Guid>;

public class CreateRolCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateRolCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRolCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await context.Roles
            .AnyAsync(r => r.NombreRol == request.NombreRol && !r.IsDeleted, cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Rol.DuplicateName", $"Ya existe un rol con el nombre '{request.NombreRol}'."));
        }

        var result = Rol.Create(request.NombreRol, request.Descripcion, request.NivelJerarquia);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Roles.Add(result.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value!.Id);
    }
}

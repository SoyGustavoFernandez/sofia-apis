using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;

public class CargaMasivaRolesCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaRolesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaRolesCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Rol.Create(row.NombreRol, row.Descripcion, row.NivelJerarquia);
            if (result.IsSuccess)
            {
                _ = context.Roles.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

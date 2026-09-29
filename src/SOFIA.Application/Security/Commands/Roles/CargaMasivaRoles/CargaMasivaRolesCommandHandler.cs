using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;

public class CargaMasivaRolesCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaRolesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaRolesCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.Roles.Where(x => !x.IsDeleted).Select(x => x.NombreRol).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Rol.Create(row.NombreRol, row.Descripcion, row.NivelJerarquia);
            if (result.IsSuccess && tomados.Add(result.Value.NombreRol))
            {
                _ = context.Roles.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

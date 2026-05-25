using Microsoft.EntityFrameworkCore;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.GetProfile;

public sealed class GetProfileQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var cuenta = await dbContext.Cuentas
            .Include(c => c.Empleado)
            .Include(c => c.Roles)
                .ThenInclude(r => r.Permisos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure<ProfileResponse>(Error.NotFound("Cuenta.NotFound", "Account not found."));
        }

        var roles = cuenta.Roles.Select(r => r.NombreRol).ToList();

        var permisos = cuenta.Roles
            .SelectMany(r => r.Permisos)
            .Select(p => new PermissionDto(p.ModuloSistema, p.Accion))
            .DistinctBy(p => new { p.Modulo, p.Accion })
            .ToList();

        var response = new ProfileResponse(
            cuenta.Id,
            cuenta.NombreUsuario,
            cuenta.Empleado?.Nombre_Completo ?? "N/A",
            roles,
            permisos);

        return Result.Success(response);
    }
}

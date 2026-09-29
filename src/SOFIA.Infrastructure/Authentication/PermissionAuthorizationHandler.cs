using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>
/// Handles authorization requirements by checking the user's roles and permissions in the database.
/// Includes a caching layer to optimize performance.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IServiceScopeFactory serviceScopeFactory,
    IPermissionCache permissionCache)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (roles.Count == 0)
        {
            return;
        }

        // Admin role bypasses all permission checks
        if (roles.Exists(r => r.Equals(Domain.Entities.Rol.AdminRoleName, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        // Role names repeat across tenants, so the cache must be partitioned by company
        if (!Guid.TryParse(context.User.FindFirstValue("empresaId"), out var empresaId))
        {
            return;
        }

        foreach (var roleName in roles)
        {
            var permissions = await GetPermissionsForRoleAsync(empresaId, roleName);

            if (permissions.Any(p =>
                p.Modulo.Equals(requirement.Module, StringComparison.OrdinalIgnoreCase) &&
                p.Accion.Equals(requirement.Action, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }

    private async Task<IReadOnlySet<(string Modulo, string Accion)>> GetPermissionsForRoleAsync(Guid empresaId, string roleName)
    {
        if (permissionCache.TryGet(empresaId, roleName, out var cached) && cached is not null)
        {
            return cached;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var permissionsFromDb = await dbContext.PermisosRol
            .AsNoTracking()
            .Where(p => p.Rol!.NombreRol == roleName)
            .Select(p => new { p.ModuloSistema, p.Accion })
            .ToListAsync();

        HashSet<(string Modulo, string Accion)> permissions = [.. permissionsFromDb.Select(p => (p.ModuloSistema.Trim(), p.Accion.Trim()))];

        permissionCache.Set(empresaId, roleName, permissions);

        return permissions;
    }
}

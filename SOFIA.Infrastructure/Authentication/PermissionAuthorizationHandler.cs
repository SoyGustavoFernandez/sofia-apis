using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Authentication;

/// <summary>
/// Handles authorization requirements by checking the user's roles and permissions in the database.
/// Includes a caching layer to optimize performance.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IServiceScopeFactory serviceScopeFactory,
    IMemoryCache memoryCache)
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

        foreach (var roleName in roles)
        {
            var permissions = await GetPermissionsForRoleAsync(roleName);

            if (permissions.Any(p =>
                p.Modulo.Equals(requirement.Module, StringComparison.OrdinalIgnoreCase) &&
                p.Accion.Equals(requirement.Action, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }

    private async Task<HashSet<(string Modulo, string Accion)>> GetPermissionsForRoleAsync(string roleName)
    {
        var cacheKey = $"permissions-{roleName}";

        if (memoryCache.TryGetValue(cacheKey, out HashSet<(string Modulo, string Accion)>? permissions) && permissions is not null)
        {
            return permissions;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var permissionsFromDb = await dbContext.PermisosRol
            .AsNoTracking()
            .Where(p => p.Rol!.NombreRol == roleName)
            .Select(p => new { p.ModuloSistema, p.Accion })
            .ToListAsync();

        permissions = [.. permissionsFromDb.Select(p => (p.ModuloSistema.Trim(), p.Accion.Trim()))];

        // Cache permissions for 30 minutes to reduce database roundtrips
        _ = memoryCache.Set(cacheKey, permissions, TimeSpan.FromMinutes(30));

        return permissions;
    }
}

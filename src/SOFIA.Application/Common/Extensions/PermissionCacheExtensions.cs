using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Common.Extensions;

public static class PermissionCacheExtensions
{
    // Roles are tenant-scoped, so the affected cache entry is the one of the caller's company
    public static void InvalidateForCurrentEmpresa(this IPermissionCache permissionCache, ICurrentUser currentUser, string roleName)
    {
        var empresaId = currentUser.GetEmpresaId();
        if (empresaId.IsSuccess)
        {
            permissionCache.Invalidate(empresaId.Value, roleName);
        }
    }
}

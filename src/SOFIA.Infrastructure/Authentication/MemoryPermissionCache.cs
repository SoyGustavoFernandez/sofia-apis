using Microsoft.Extensions.Caching.Memory;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Authentication;

public sealed class MemoryPermissionCache(IMemoryCache memoryCache) : IPermissionCache
{
    // Explicit eviction only reaches this instance, so a short TTL bounds staleness on multi-instance deployments
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public bool TryGet(Guid empresaId, string roleName, out IReadOnlySet<(string Modulo, string Accion)>? permissions) =>
        memoryCache.TryGetValue(BuildKey(empresaId, roleName), out permissions) && permissions is not null;

    public void Set(Guid empresaId, string roleName, IReadOnlySet<(string Modulo, string Accion)> permissions) =>
        _ = memoryCache.Set(BuildKey(empresaId, roleName), permissions, Ttl);

    public void Invalidate(Guid empresaId, string roleName) => memoryCache.Remove(BuildKey(empresaId, roleName));

    // Role names repeat across tenants and SQL compares them case-insensitively, so the key is company-scoped and case-folded
    private static string BuildKey(Guid empresaId, string roleName) => $"permissions:{empresaId}:{roleName.ToUpperInvariant()}";
}

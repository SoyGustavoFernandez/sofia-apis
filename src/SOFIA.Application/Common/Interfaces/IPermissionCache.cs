namespace SOFIA.Application.Common.Interfaces;

// Per-company cache of role permissions used by the authorization handler; evict it whenever a role's permissions change
public interface IPermissionCache
{
    bool TryGet(Guid empresaId, string roleName, out IReadOnlySet<(string Modulo, string Accion)>? permissions);

    void Set(Guid empresaId, string roleName, IReadOnlySet<(string Modulo, string Accion)> permissions);

    void Invalidate(Guid empresaId, string roleName);
}

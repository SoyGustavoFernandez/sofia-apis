namespace SOFIA.Application.Common.Interfaces;

// Branches the current account may read or operate on: base branch plus account and role assignments
public interface ISucursalAccess
{
    // Null means every branch of the tenant (Admin)
    Task<IReadOnlySet<Guid>?> GetAllowedSucursalesAsync(CancellationToken cancellationToken);

    Task<bool> CanAccessAsync(Guid sucursalId, CancellationToken cancellationToken);
}

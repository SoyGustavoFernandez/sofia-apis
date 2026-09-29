using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Common;

public static class ProveedorDuplicateChecker
{
    public const string TaxIdDuplicado = "Proveedor.TaxId.Duplicado";
    public const string RazonSocialDuplicada = "Proveedor.RazonSocial.Duplicado";

    // Tenant scoping comes from the global query filter; proveedorId excludes the row being updated
    public static async Task<Error?> FindAsync(IApplicationDbContext context, Guid? proveedorId, string razonSocial, string taxId, CancellationToken cancellationToken)
    {
        var taxIdTomado = await context.Proveedores
            .AnyAsync(p => p.Id != proveedorId && p.TaxId == taxId && !p.IsDeleted, cancellationToken);
        if (taxIdTomado)
        {
            return Error.Conflict(TaxIdDuplicado, "Another supplier already uses this tax id.");
        }

        var razonSocialTomada = await context.Proveedores
            .AnyAsync(p => p.Id != proveedorId && p.RazonSocial == razonSocial && !p.IsDeleted, cancellationToken);
        return razonSocialTomada ? Error.Conflict(RazonSocialDuplicada, "Another supplier already uses this business name.") : null;
    }
}

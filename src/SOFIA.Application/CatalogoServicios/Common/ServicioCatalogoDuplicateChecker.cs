using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Common;

public static class ServicioCatalogoDuplicateChecker
{
    public const string NombreDuplicado = "ServicioCatalogo.Nombre.Duplicado";

    // Unique per company: the global query filter scopes the lookup
    public static async Task<Error?> FindAsync(IApplicationDbContext context, Guid? servicioId, string nombre, CancellationToken cancellationToken) =>
        await context.ServiciosCatalogo.AnyAsync(s => s.Id != servicioId && s.Nombre == nombre && !s.IsDeleted, cancellationToken)
            ? Error.Conflict(NombreDuplicado, "Another service already uses this name.")
            : null;
}

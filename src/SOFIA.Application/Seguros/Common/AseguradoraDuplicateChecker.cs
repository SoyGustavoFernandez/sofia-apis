using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Common;

public static class AseguradoraDuplicateChecker
{
    public const string CodigoDuplicado = "Aseguradora.Codigo.Duplicado";

    // Unique per company: the global query filter scopes the lookup, so other companies may use the same code
    public static async Task<Error?> FindAsync(IApplicationDbContext context, Guid? aseguradoraId, string codigo, CancellationToken cancellationToken) =>
        await context.Aseguradoras.AnyAsync(a => a.Id != aseguradoraId && a.CodigoIdentificadorNacional == codigo && !a.IsDeleted, cancellationToken)
            ? Error.Conflict(CodigoDuplicado, "Another insurer already uses this national code.")
            : null;
}

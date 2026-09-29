using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Common;

public static class LaboratorioDuplicateChecker
{
    public const string NombreDuplicado = "Laboratorio.NombreCompania.Duplicado";
    public const string CodigoDuplicado = "Laboratorio.CodigoIdentificador.Duplicado";

    // Tenant scoping comes from the global query filter; laboratorioId excludes the row being updated
    public static async Task<Error?> FindAsync(IApplicationDbContext context, Guid? laboratorioId, string nombreCompania, string? codigoIdentificador, CancellationToken cancellationToken)
    {
        var nombreTomado = await context.Laboratorios
            .AnyAsync(l => l.Id != laboratorioId && l.NombreCompania == nombreCompania && !l.IsDeleted, cancellationToken);
        if (nombreTomado)
        {
            return Error.Conflict(NombreDuplicado, "Another laboratory already uses this company name.");
        }

        if (codigoIdentificador is null)
        {
            return null;
        }

        var codigoTomado = await context.Laboratorios
            .AnyAsync(l => l.Id != laboratorioId && l.CodigoIdentificador == codigoIdentificador && !l.IsDeleted, cancellationToken);
        return codigoTomado ? Error.Conflict(CodigoDuplicado, "Another laboratory already uses this identifier code.") : null;
    }
}

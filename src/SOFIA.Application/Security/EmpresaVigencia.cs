using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security;

public static class EmpresaVigencia
{
    public static readonly Error NoVigenteError =
        Error.Forbidden("Auth.EmpresaNoVigente", "The company's subscription is not active.");

    // Missing, soft-deleted, suspended, cancelled or expired companies all fail closed
    public static async Task<bool> EmpresaEstaVigenteAsync(this IApplicationDbContext context, Guid? empresaId, CancellationToken cancellationToken)
    {
        if (empresaId is null)
        {
            return false;
        }

        var empresa = await context.Empresas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == empresaId.Value, cancellationToken);

        return empresa?.EstaVigenteEn(DateTimeOffset.UtcNow) ?? false;
    }
}

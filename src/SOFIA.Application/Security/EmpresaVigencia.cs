using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security;

public static class EmpresaVigencia
{
    public static readonly Error NoVigenteError =
        Error.Forbidden("Auth.EmpresaNoVigente", "The company's subscription is not active.");

    // The platform operator is never locked out by its own company: nobody else could reactivate it
    public static async Task<bool> PuedeOperarAsync(this IApplicationDbContext context, Cuenta cuenta, CancellationToken cancellationToken) =>
        cuenta.EsAdminPlataforma || await context.EmpresaEstaVigenteAsync(cuenta.TenantId, cancellationToken);

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

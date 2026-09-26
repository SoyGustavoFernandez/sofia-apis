using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Security;

internal static class RefreshTokenRevocation
{
    // Tokens are issued anonymously (login/sign-up) with no tenant, so the tenant filter would hide them all
    internal static async Task RevokeAllRefreshTokensAsync(this IApplicationDbContext context, Guid cuentaId, CancellationToken cancellationToken)
    {
        var activeTokens = await context.RefreshTokens
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Where(rt => rt.CuentaId == cuentaId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }
}

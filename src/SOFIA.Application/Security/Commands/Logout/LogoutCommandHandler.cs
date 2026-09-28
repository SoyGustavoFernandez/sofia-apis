using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Logout;

public class LogoutCommandHandler(IApplicationDbContext context) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var cuentaIds = new HashSet<Guid>();
        if (request.CuentaId is { } cuentaId && cuentaId != Guid.Empty)
        {
            _ = cuentaIds.Add(cuentaId);
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var hash = TokenHasher.HashToken(request.RefreshToken);
            var now = DateTimeOffset.UtcNow;

            // Tenant-less lookup (anonymous request); a rotated cookie still counts so a racing refresh cannot keep the session alive
            var stored = await context.RefreshTokens
                .IgnoreQueryFilters([QueryFilters.Tenant])
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash && rt.ExpiresAt > now, cancellationToken);

            if (stored is not null)
            {
                _ = cuentaIds.Add(stored.CuentaId);
            }
        }

        // Idempotent: nothing to revoke still succeeds, so the endpoint does not reveal whether a session existed
        if (cuentaIds.Count == 0)
        {
            return Result.Success();
        }

        var cuentas = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Where(c => cuentaIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var id in cuentaIds)
        {
            await context.RevokeAllRefreshTokensAsync(id, cancellationToken);
        }

        foreach (var cuenta in cuentas)
        {
            cuenta.InvalidateSecurityStamp();
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Logout;

public class LogoutCommandHandler(IApplicationDbContext context) : IRequestHandler<LogoutCommand, Result>
{
    private const int MaxAttempts = 3;

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

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await RevokeAsync(cuentaIds, cuentas, cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
            {
                // A racing refresh rotated a token meanwhile: reload it and revoke again, including the token it just issued
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
        }

        return Result.Success();
    }

    private async Task RevokeAsync(HashSet<Guid> cuentaIds, List<Cuenta> cuentas, CancellationToken cancellationToken)
    {
        foreach (var id in cuentaIds)
        {
            await context.RevokeAllRefreshTokensAsync(id, cancellationToken);
        }

        foreach (var cuenta in cuentas)
        {
            cuenta.InvalidateSecurityStamp();
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }
}

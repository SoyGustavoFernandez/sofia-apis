using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Common;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Security.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IJwtProvider jwtProvider,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, Result<LoginResult>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.HashToken(request.Token);
        var now = DateTimeOffset.UtcNow;

        // Anonymous request (cookie only): the tenant is resolved from the account itself
        var stored = await context.RefreshTokens
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (stored is null || stored.ExpiresAt <= now)
        {
            return InvalidToken();
        }

        if (stored.IsRevoked)
        {
            if (stored.IsReuseAttempt(now))
            {
                await RevokeSessionAsync(stored.CuentaId, cancellationToken);
            }

            return InvalidToken();
        }

        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Include(c => c.Roles)
            .Include(c => c.Empleado).ThenInclude(e => e!.Sucursal_Base)
            .FirstOrDefaultAsync(c => c.Id == stored.CuentaId && c.CuentaActiva && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return InvalidToken();
        }

        // Rotate: revoke old token and issue a new pair
        stored.Revoke();

        var accessToken = jwtProvider.Generate(cuenta);
        var (rawToken, tokenHash) = TokenHasher.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);
        var newRefreshToken = DomainRefreshToken.Create(stored.CuentaId, tokenHash, expiry);
        _ = context.RefreshTokens.Add(newRefreshToken);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResult(accessToken, rawToken, expiry));
    }

    // A replayed rotated token means the cookie was copied: end every session of the account, including live access tokens
    private async Task RevokeSessionAsync(Guid cuentaId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Refresh token reuse detected for account {CuentaId}; revoking all sessions.", cuentaId);

        await context.RevokeAllRefreshTokensAsync(cuentaId, cancellationToken);

        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(c => c.Id == cuentaId, cancellationToken);
        cuenta?.InvalidateSecurityStamp();

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static Result<LoginResult> InvalidToken() => Result.Failure<LoginResult>(
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."), 401);
}

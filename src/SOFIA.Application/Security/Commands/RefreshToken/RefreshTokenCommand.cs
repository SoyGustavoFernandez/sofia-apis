using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Common;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Security.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : ICommand<LoginResult>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => _ = RuleFor(x => x.Token).NotEmpty();
}

public class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IJwtProvider jwtProvider) : IRequestHandler<RefreshTokenCommand, Result<LoginResult>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.HashToken(request.Token);

        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == hash && !rt.IsRevoked && rt.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

        if (stored is null)
        {
            return Result.Failure<LoginResult>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."), 401);
        }

        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .Include(c => c.Empleado).ThenInclude(e => e!.Sucursal_Base)
            .FirstOrDefaultAsync(c => c.Id == stored.CuentaId && c.CuentaActiva && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure<LoginResult>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."), 401);
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
}

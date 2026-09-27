using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Security.Commands.Login;

public class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    ICurrentUser currentUser,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    // Built lazily with the real hasher so unknown usernames cost the same as known ones
    private static string? _dummyHash;

    private static string GetDummyHash(IPasswordHasher hasher) => _dummyHash ??= hasher.Hash(Guid.NewGuid().ToString("N"));

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ip = currentUser.ClientIpAddress ?? "unknown";

        // Anonymous request: the tenant is resolved from the account itself
        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Include(c => c.Roles)
            .Include(c => c.Empleado).ThenInclude(e => e!.Sucursal_Base).ThenInclude(s => s!.Empresa)
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        // Always pay the BCrypt cost so response time does not reveal whether the account exists
        var passwordValid = passwordHasher.Verify(request.Password, cuenta?.PasswordHash ?? GetDummyHash(passwordHasher));

        if (cuenta is null || !cuenta.CuentaActiva)
        {
            logger.LogWarning("Failed login attempt for username {Username} from IP {IpAddress} â€” account not found or inactive.", request.NombreUsuario, ip);
            return Result.Failure<LoginResult>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        if (cuenta.BloqueadoHasta > DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Blocked login attempt for username {Username} from IP {IpAddress} â€” account locked until {LockedUntil}.", request.NombreUsuario, ip, cuenta.BloqueadoHasta);
            // Same response as bad credentials, otherwise the lock state confirms the account exists
            return Result.Failure<LoginResult>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        if (!passwordValid)
        {
            cuenta.RegisterFailedAttempt();
            _ = await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Failed login attempt for username {Username} from IP {IpAddress} â€” invalid password. Failed attempts: {FailedAttempts}.", request.NombreUsuario, ip, cuenta.IntentosFallidos);
            return Result.Failure<LoginResult>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        cuenta.ResetFailedAttempts();

        var accessToken = jwtProvider.Generate(cuenta);
        var (rawToken, tokenHash) = TokenHasher.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);
        var refreshToken = DomainRefreshToken.Create(cuenta.Id, tokenHash, expiry);
        _ = context.RefreshTokens.Add(refreshToken);

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successful login for username {Username} from IP {IpAddress}.", request.NombreUsuario, ip);
        return Result.Success(new LoginResult(accessToken, rawToken, expiry));
    }
}

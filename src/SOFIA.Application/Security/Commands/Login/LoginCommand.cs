using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Login;

public record LoginCommand(string NombreUsuario, string Password) : ICommand<string>;

public class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    ICurrentUser currentUser,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ip = currentUser.ClientIpAddress ?? "unknown";

        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .Include(c => c.Empleado).ThenInclude(e => e!.Sucursal_Base)
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (cuenta is null || !cuenta.CuentaActiva)
        {
            logger.LogWarning("Failed login attempt for username {Username} from IP {IpAddress} — account not found or inactive.", request.NombreUsuario, ip);
            return Result.Failure<string>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        if (cuenta.BloqueadoHasta > DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Blocked login attempt for username {Username} from IP {IpAddress} — account locked until {LockedUntil}.", request.NombreUsuario, ip, cuenta.BloqueadoHasta);
            return Result.Failure<string>(Error.Forbidden("Auth.Blocked", $"Account locked until {cuenta.BloqueadoHasta}."), 403);
        }

        if (!passwordHasher.Verify(request.Password, cuenta.PasswordHash))
        {
            cuenta.RegisterFailedAttempt();
            _ = await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Failed login attempt for username {Username} from IP {IpAddress} — invalid password. Failed attempts: {FailedAttempts}.", request.NombreUsuario, ip, cuenta.IntentosFallidos);
            return Result.Failure<string>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        cuenta.ResetFailedAttempts();
        _ = await context.SaveChangesAsync(cancellationToken);

        var token = jwtProvider.Generate(cuenta);

        logger.LogInformation("Successful login for username {Username} from IP {IpAddress}.", request.NombreUsuario, ip);
        return Result.Success(token);
    }
}

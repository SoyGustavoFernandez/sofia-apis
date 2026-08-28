using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Login;

public record LoginCommand(string NombreUsuario, string Password) : ICommand<string>;

public class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider) : IRequestHandler<LoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .Include(c => c.Empleado).ThenInclude(e => e!.Sucursal_Base)
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (cuenta is null || !cuenta.CuentaActiva)
        {
            return Result.Failure<string>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        if (cuenta.BloqueadoHasta > DateTimeOffset.UtcNow)
        {
            return Result.Failure<string>(Error.Forbidden("Auth.Blocked", $"Cuenta bloqueada hasta {cuenta.BloqueadoHasta}."), 403);
        }

        if (!passwordHasher.Verify(request.Password, cuenta.PasswordHash))
        {
            cuenta.RegisterFailedAttempt();
            _ = await context.SaveChangesAsync(cancellationToken);
            return Result.Failure<string>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        cuenta.ResetFailedAttempts();
        _ = await context.SaveChangesAsync(cancellationToken);

        var token = jwtProvider.Generate(cuenta);

        return Result.Success(token);
    }
}

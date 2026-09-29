using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Same tenant-less lookup as login and refresh: the account id comes from the validated token
        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && c.CuentaActiva && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password."), 401);
        }

        // 400, not 401: a wrong current password must not look like an expired session to the client
        if (!passwordHasher.Verify(request.CurrentPassword, cuenta.PasswordHash))
        {
            return Result.Failure(Error.Validation("Auth.ClaveActualIncorrecta", "The current password is incorrect."));
        }

        // Rotates the security stamp, so every live access token (this one included) stops working
        cuenta.UpdatePassword(passwordHasher.Hash(request.NewPassword));

        await context.RevokeAllRefreshTokensAsync(cuenta.Id, cancellationToken);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

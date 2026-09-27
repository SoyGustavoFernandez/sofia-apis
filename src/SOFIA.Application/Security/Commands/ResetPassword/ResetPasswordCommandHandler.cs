using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        // Hash before the existence check so response time does not reveal whether the account exists
        var passwordHash = passwordHasher.Hash(request.NewPassword);

        if (cuenta is null)
        {
            return Result.Failure(Cuenta.InvalidRecoveryTokenError);
        }

        var result = cuenta.ResetPassword(request.Token, passwordHash);

        if (result.IsFailure)
        {
            return result;
        }

        // A stolen refresh token must not outlive the password it was obtained with
        await context.RevokeAllRefreshTokensAsync(cuenta.Id, cancellationToken);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

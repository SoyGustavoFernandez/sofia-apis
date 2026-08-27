using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ForgotPassword;

public record ForgotPasswordCommand(string NombreUsuario) : ICommand<string>;

public class ForgotPasswordCommandHandler(IApplicationDbContext context) : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            // Standard security practice: avoid account enumeration by returning a generic message.
            return Result.Failure<string>(Error.NotFound("Auth.CuentaNotFound", "Si el usuario existe, se ha generado un token."));
        }

        cuenta.GenerateRecoveryToken();
        _ = await context.SaveChangesAsync(cancellationToken);

        // TODO: Integrate with IEmailService to send recovery token to user's registered contact.
        return Result.Success(cuenta.RecoveryToken!);
    }
}

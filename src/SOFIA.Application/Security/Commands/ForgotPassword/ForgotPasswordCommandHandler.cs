using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IFrontendLinks frontendLinks,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .Include(c => c.Empleado)
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        // Inactive accounts, employees without email and companies without an active subscription get no token, but the same response
        if (cuenta is { CuentaActiva: true, Empleado: { IsDeleted: false, Email: { } email } }
            && await context.EmpresaEstaVigenteAsync(cuenta.TenantId, cancellationToken))
        {
            var (rawToken, tokenHash) = TokenHasher.GenerateRecoveryToken();
            cuenta.GenerateRecoveryToken(tokenHash);
            _ = await context.SaveChangesAsync(cancellationToken);

            var message = PasswordRecoveryEmail.Build(email, cuenta.Empleado.Nombres, cuenta.NombreUsuario, frontendLinks.PasswordReset(rawToken, cuenta.NombreUsuario));
            var sent = await emailSender.SendAsync(message, cancellationToken);
            if (sent.IsFailure)
            {
                // The token stays valid for its hour; the user can simply ask again
                logger.LogWarning("Password recovery email for account {CuentaId} could not be sent: {ErrorCode}", cuenta.Id, sent.Error.Code);
            }
        }

        // Always succeed — never reveal whether the account exists (prevents user enumeration).
        return Result.Success();
    }
}

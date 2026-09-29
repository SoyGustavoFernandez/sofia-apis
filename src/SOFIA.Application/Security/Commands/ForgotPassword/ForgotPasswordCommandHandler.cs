using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(IApplicationDbContext context) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        // Inactive accounts get no token, but the same response as any other request
        if (cuenta is { CuentaActiva: true })
        {
            var (_, tokenHash) = TokenHasher.GenerateRecoveryToken();
            cuenta.GenerateRecoveryToken(tokenHash);
            _ = await context.SaveChangesAsync(cancellationToken);
            // TODO: Integrate with IEmailService to send the raw recovery token (discarded above) to the user's registered contact.
        }

        // Always succeed â€” never reveal whether the account exists (prevents user enumeration).
        return Result.Success();
    }
}

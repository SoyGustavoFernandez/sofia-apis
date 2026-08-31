using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(IApplicationDbContext context) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .FirstOrDefaultAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (cuenta is not null)
        {
            cuenta.GenerateRecoveryToken();
            _ = await context.SaveChangesAsync(cancellationToken);
            // TODO: Integrate with IEmailService to send recovery token to user's registered contact.
        }

        // Always succeed â€” never reveal whether the account exists (prevents user enumeration).
        return Result.Success();
    }
}

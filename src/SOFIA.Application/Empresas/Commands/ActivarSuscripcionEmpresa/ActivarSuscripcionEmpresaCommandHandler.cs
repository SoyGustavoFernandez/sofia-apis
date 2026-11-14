using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;

public class ActivarSuscripcionEmpresaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<ActivarSuscripcionEmpresaCommand, Result>
{
    public async Task<Result> Handle(ActivarSuscripcionEmpresaCommand request, CancellationToken cancellationToken)
    {
        // Checked here too because this handler reaches outside the caller's tenant
        if (!currentUser.EsAdminPlataforma)
        {
            return Result.Failure(PlataformaAdminPolicy.RequiredError, 403);
        }

        var empresa = await context.Empresas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (empresa is null)
        {
            return Result.Failure(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404);
        }

        var result = empresa.ActivarSuscripcion(request.FechaVencimiento);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

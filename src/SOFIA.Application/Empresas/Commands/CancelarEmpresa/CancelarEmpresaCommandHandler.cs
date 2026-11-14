using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Empresas.Common;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Commands.CancelarEmpresa;

public class CancelarEmpresaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CancelarEmpresaCommand, Result>
{
    public async Task<Result> Handle(CancelarEmpresaCommand request, CancellationToken cancellationToken)
    {
        // Checked here too because this handler reaches outside the caller's tenant
        if (!currentUser.EsAdminPlataforma)
        {
            return Result.Failure(PlataformaAdminPolicy.RequiredError, 403);
        }

        // Blocking the operator's own company would lock out the only account able to undo it
        if (currentUser.EsSuPropiaEmpresa(request.Id))
        {
            return Result.Failure(EmpresaPlataformaExtensions.PropiaEmpresaError, 409);
        }

        var empresa = await context.Empresas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (empresa is null)
        {
            return Result.Failure(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404);
        }

        empresa.Cancelar();

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

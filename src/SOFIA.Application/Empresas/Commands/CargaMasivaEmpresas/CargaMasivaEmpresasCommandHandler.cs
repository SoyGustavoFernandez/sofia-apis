using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Commands.CargaMasivaEmpresas;

public class CargaMasivaEmpresasCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaEmpresasCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaEmpresasCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Empresa.Create(row.Nombre, row.RUC);
            if (result.IsSuccess)
            {
                _ = context.Empresas.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

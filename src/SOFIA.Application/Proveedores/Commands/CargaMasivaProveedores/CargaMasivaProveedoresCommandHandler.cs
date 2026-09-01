using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;

public class CargaMasivaProveedoresCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaProveedoresCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaProveedoresCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = ProveedorDistribuidor.Create(row.RazonSocial, row.TaxId, row.TerminosFinancieros, row.CalificacionEsg, row.TasaCumplimiento);
            if (result.IsSuccess)
            {
                _ = context.Proveedores.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

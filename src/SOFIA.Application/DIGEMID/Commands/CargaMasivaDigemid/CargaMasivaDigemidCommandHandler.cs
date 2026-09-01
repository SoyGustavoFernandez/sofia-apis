using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;

public class CargaMasivaDigemidCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaDigemidCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaDigemidCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = DigemidCatalogoProducto.Create(
                row.CodProd,
                row.NomProd,
                row.Concent,
                row.FormaFarmaceutica,
                row.Fraccion,
                row.RegistroSanitario,
                row.Titular,
                row.Estado);

            if (result.IsSuccess)
            {
                _ = context.DigemidCatalogoProductos.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

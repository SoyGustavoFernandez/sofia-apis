using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;

public class CargaMasivaDigemidCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaDigemidCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaDigemidCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.DigemidCatalogoProductos.Where(x => !x.IsDeleted).Select(x => x.CodProd).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

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

            if (result.IsSuccess && tomados.Add(result.Value.CodProd))
            {
                _ = context.DigemidCatalogoProductos.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

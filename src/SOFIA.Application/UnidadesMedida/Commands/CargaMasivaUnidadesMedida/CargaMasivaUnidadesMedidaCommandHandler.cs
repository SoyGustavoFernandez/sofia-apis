using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;

public class CargaMasivaUnidadesMedidaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaUnidadesMedidaCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaUnidadesMedidaCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.UnidadesMedida.Where(x => !x.IsDeleted).Select(x => x.Codigo).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = UnidadMedida.Create(row.Codigo, row.Descripcion);
            if (result.IsSuccess && tomados.Add(result.Value.Codigo))
            {
                _ = context.UnidadesMedida.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

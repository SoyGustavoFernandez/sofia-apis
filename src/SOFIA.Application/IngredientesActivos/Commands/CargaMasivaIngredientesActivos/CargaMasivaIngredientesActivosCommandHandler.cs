using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;

public class CargaMasivaIngredientesActivosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaIngredientesActivosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaIngredientesActivosCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.IngredientesActivos.Where(x => !x.IsDeleted).Select(x => x.DenominacionDci).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = IngredienteActivo.Create(row.DenominacionDci, row.CodigoAtc);
            if (result.IsSuccess && tomados.Add(result.Value.DenominacionDci))
            {
                _ = context.IngredientesActivos.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

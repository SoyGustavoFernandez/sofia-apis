using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;

public class CargaMasivaIngredientesActivosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaIngredientesActivosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaIngredientesActivosCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = IngredienteActivo.Create(row.DenominacionDci, row.CodigoAtc);
            if (result.IsSuccess)
            {
                _ = context.IngredientesActivos.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

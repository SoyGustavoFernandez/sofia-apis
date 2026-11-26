using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Seguros.Commands.CargaMasivaSeguros;

public class CargaMasivaSegurosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaSegurosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaSegurosCommand request, CancellationToken cancellationToken)
    {
        // Codes already used by this company or by an earlier row of the file are skipped like any other invalid row
        var codigos = new HashSet<string>(
            await context.Aseguradoras.Where(a => !a.IsDeleted).Select(a => a.CodigoIdentificadorNacional).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = AseguradoraMedica.Create(row.NombreComercial, row.CodigoIdentificadorNacional);
            if (result.IsSuccess && codigos.Add(result.Value.CodigoIdentificadorNacional))
            {
                _ = context.Aseguradoras.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

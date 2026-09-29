using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;

public class CargaMasivaSucursalesCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CargaMasivaSucursalesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaSucursalesCommand request, CancellationToken cancellationToken)
    {
        var empresaResult = currentUser.GetEmpresaId();
        if (empresaResult.IsFailure)
        {
            return Result.Failure<int>(empresaResult.Error);
        }

        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.Sucursales.Where(x => !x.IsDeleted).Select(x => x.Numero_Licencia).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Sucursal.Create(row.Nombre, row.DireccionFisica, row.NumeroLicencia, null, empresaResult.Value);
            if (result.IsSuccess && tomados.Add(result.Value.Numero_Licencia))
            {
                _ = context.Sucursales.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

using MediatR;
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

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Sucursal.Create(row.Nombre, row.DireccionFisica, row.NumeroLicencia, null, empresaResult.Value);
            if (result.IsSuccess)
            {
                _ = context.Sucursales.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

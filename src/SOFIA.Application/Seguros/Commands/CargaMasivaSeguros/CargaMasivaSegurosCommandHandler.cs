using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Seguros.Commands.CargaMasivaSeguros;

public class CargaMasivaSegurosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaSegurosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaSegurosCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = AseguradoraMedica.Create(row.NombreComercial, row.CodigoIdentificadorNacional);
            if (result.IsSuccess)
            {
                _ = context.Aseguradoras.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

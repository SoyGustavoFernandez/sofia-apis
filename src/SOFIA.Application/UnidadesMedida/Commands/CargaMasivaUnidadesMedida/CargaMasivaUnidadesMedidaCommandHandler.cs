using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;

public class CargaMasivaUnidadesMedidaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaUnidadesMedidaCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaUnidadesMedidaCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = UnidadMedida.Create(row.Codigo, row.Descripcion);
            if (result.IsSuccess)
            {
                _ = context.UnidadesMedida.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

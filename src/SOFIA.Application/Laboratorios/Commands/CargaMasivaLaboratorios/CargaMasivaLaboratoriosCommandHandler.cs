using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;

public class CargaMasivaLaboratoriosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaLaboratoriosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaLaboratoriosCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Laboratorio.Create(row.NombreCompania, row.CodigoIdentificador);
            if (result.IsSuccess)
            {
                _ = context.Laboratorios.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

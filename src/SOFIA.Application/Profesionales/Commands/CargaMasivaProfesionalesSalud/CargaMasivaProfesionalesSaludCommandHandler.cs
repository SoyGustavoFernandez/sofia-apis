using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;

public class CargaMasivaProfesionalesSaludCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaProfesionalesSaludCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaProfesionalesSaludCommand request, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = ProfesionalSalud.Create(row.NumeroRegistro, row.NombrePrescriptor, row.DireccionClinica);
            if (result.IsSuccess)
            {
                _ = context.ProfesionalesSalud.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

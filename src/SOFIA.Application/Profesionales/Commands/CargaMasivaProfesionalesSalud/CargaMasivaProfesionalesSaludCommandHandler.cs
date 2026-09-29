using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;

public class CargaMasivaProfesionalesSaludCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaProfesionalesSaludCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaProfesionalesSaludCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.ProfesionalesSalud.Where(x => !x.IsDeleted).Select(x => x.NumeroRegistro).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = ProfesionalSalud.Create(row.NumeroRegistro, row.NombrePrescriptor, row.DireccionClinica);
            if (result.IsSuccess && tomados.Add(result.Value.NumeroRegistro))
            {
                _ = context.ProfesionalesSalud.Add(result.Value);
                saved++;
            }
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;

public class CargaMasivaLaboratoriosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaLaboratoriosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaLaboratoriosCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var nombres = new HashSet<string>(
            await context.Laboratorios.Where(l => !l.IsDeleted).Select(l => l.NombreCompania).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var codigos = new HashSet<string>(
            await context.Laboratorios.Where(l => l.CodigoIdentificador != null && !l.IsDeleted).Select(l => l.CodigoIdentificador!).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = Laboratorio.Create(row.NombreCompania, row.CodigoIdentificador);
            if (!result.IsSuccess)
            {
                continue;
            }

            var laboratorio = result.Value;
            if (nombres.Contains(laboratorio.NombreCompania) || (laboratorio.CodigoIdentificador is not null && codigos.Contains(laboratorio.CodigoIdentificador)))
            {
                continue;
            }

            _ = nombres.Add(laboratorio.NombreCompania);
            if (laboratorio.CodigoIdentificador is not null)
            {
                _ = codigos.Add(laboratorio.CodigoIdentificador);
            }

            _ = context.Laboratorios.Add(laboratorio);
            saved++;
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

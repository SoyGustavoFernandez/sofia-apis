using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;

public class CargaMasivaProveedoresCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaProveedoresCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaProveedoresCommand request, CancellationToken cancellationToken)
    {
        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var existentes = await context.Proveedores
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.TaxId, p.RazonSocial })
            .ToListAsync(cancellationToken);
        var taxIds = new HashSet<string>(existentes.Select(p => p.TaxId), StringComparer.OrdinalIgnoreCase);
        var razonesSociales = new HashSet<string>(existentes.Select(p => p.RazonSocial), StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            var result = ProveedorDistribuidor.Create(row.RazonSocial, row.TaxId, row.TerminosFinancieros, row.CalificacionEsg, row.TasaCumplimiento);
            if (!result.IsSuccess || taxIds.Contains(result.Value.TaxId) || razonesSociales.Contains(result.Value.RazonSocial))
            {
                continue;
            }

            _ = taxIds.Add(result.Value.TaxId);
            _ = razonesSociales.Add(result.Value.RazonSocial);
            _ = context.Proveedores.Add(result.Value);
            saved++;
        }
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

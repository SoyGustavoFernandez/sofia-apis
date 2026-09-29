using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;

public class CargaMasivaMedicamentosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaMedicamentosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaMedicamentosCommand request, CancellationToken cancellationToken)
    {
        var laboratorios = new ImportNameLookup((await context.Laboratorios
            .Select(l => new { l.NombreCompania, l.Id })
            .ToListAsync(cancellationToken)).Select(l => (l.NombreCompania, l.Id)));
        var unidades = new ImportNameLookup((await context.UnidadesMedida
            .Select(u => new { u.Descripcion, u.Id })
            .ToListAsync(cancellationToken)).Select(u => (u.Descripcion, u.Id)));

        // Keys already taken in the database or by an earlier row of the file are skipped like any other invalid row
        var tomados = new HashSet<string>(
            await context.Medicamentos.Where(x => !x.IsDeleted).Select(x => x.CodigoNacional).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            // Unknown or ambiguous names are skipped; the preview already reported them as row errors
            if (!laboratorios.TryGetUnique(row.Laboratorio, out var laboratorioId))
            {
                continue;
            }

            if (!unidades.TryGetUnique(row.UnidadBase, out var unidadBaseId))
            {
                continue;
            }

            var condicionIndex = Array.FindIndex(
                Medicamento.CondicionesValidas,
                c => string.Equals(c, row.CondicionVenta, StringComparison.OrdinalIgnoreCase));
            if (condicionIndex < 0)
            {
                continue;
            }

            var result = Medicamento.Create(
                row.CodigoNacional,
                row.NombreComercial,
                laboratorioId,
                unidadBaseId,
                (Domain.Enums.CondicionVenta)condicionIndex);

            if (result.IsSuccess && tomados.Add(result.Value.CodigoNacional))
            {
                _ = context.Medicamentos.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

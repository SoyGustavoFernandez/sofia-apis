using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;

public class CargaMasivaMedicamentosCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaMedicamentosCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaMedicamentosCommand request, CancellationToken cancellationToken)
    {
        var laboratorios = await context.Laboratorios
            .ToDictionaryAsync(l => l.NombreCompania.ToLower(), l => l.Id, cancellationToken);
        var unidades = await context.UnidadesMedida
            .ToDictionaryAsync(u => u.Descripcion.ToLower(), u => u.Id, cancellationToken);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            if (!laboratorios.TryGetValue((row.Laboratorio ?? string.Empty).ToLower(), out var laboratorioId))
            {
                continue;
            }

            if (!unidades.TryGetValue((row.UnidadBase ?? string.Empty).ToLower(), out var unidadBaseId))
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

            if (result.IsSuccess)
            {
                _ = context.Medicamentos.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

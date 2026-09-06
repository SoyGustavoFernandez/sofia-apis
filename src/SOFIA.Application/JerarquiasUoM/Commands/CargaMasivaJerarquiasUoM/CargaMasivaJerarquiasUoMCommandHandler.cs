using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;

public class CargaMasivaJerarquiasUoMCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaJerarquiasUoMCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaJerarquiasUoMCommand request, CancellationToken cancellationToken)
    {
        var productos = await context.Medicamentos
            .Where(m => !m.IsDeleted)
            .ToDictionaryAsync(m => m.NombreComercial.ToLower(), m => m.Id, cancellationToken);
        var unidades = await context.UnidadesMedida
            .ToDictionaryAsync(u => u.Descripcion.ToLower(), u => u.Id, cancellationToken);

        var saved = 0;
        foreach (var row in request.Rows)
        {
            if (!productos.TryGetValue((row.Producto ?? string.Empty).ToLower(), out var productoId))
            {
                continue;
            }

            if (!unidades.TryGetValue((row.UnidadMayor ?? string.Empty).ToLower(), out var unidadMayorId))
            {
                continue;
            }

            if (!unidades.TryGetValue((row.UnidadMenor ?? string.Empty).ToLower(), out var unidadMenorId))
            {
                continue;
            }

            if (!decimal.TryParse(row.Multiplicador, NumberStyles.Number, CultureInfo.InvariantCulture, out var multiplicador))
            {
                continue;
            }

            var result = JerarquiaUoM.Create(productoId, unidadMayorId, unidadMenorId, multiplicador);
            if (result.IsSuccess)
            {
                _ = context.JerarquiasUoM.Add(result.Value);
                saved++;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

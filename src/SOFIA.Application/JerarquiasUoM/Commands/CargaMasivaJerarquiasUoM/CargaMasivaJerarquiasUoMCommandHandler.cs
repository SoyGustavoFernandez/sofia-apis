using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;

public class CargaMasivaJerarquiasUoMCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CargaMasivaJerarquiasUoMCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CargaMasivaJerarquiasUoMCommand request, CancellationToken cancellationToken)
    {
        var productos = new ImportNameLookup((await context.Medicamentos
            .Where(m => !m.IsDeleted)
            .Select(m => new { m.NombreComercial, m.Id })
            .ToListAsync(cancellationToken)).Select(m => (m.NombreComercial, m.Id)));
        var unidades = new ImportNameLookup((await context.UnidadesMedida
            .Select(u => new { u.Descripcion, u.Id })
            .ToListAsync(cancellationToken)).Select(u => (u.Descripcion, u.Id)));

        var candidates = new List<JerarquiaUoM>();
        foreach (var row in request.Rows)
        {
            // Unknown or ambiguous names are skipped; the preview already reported them as row errors
            if (!productos.TryGetUnique(row.Producto, out var productoId)
                || !unidades.TryGetUnique(row.UnidadMayor, out var unidadMayorId)
                || !unidades.TryGetUnique(row.UnidadMenor, out var unidadMenorId)
                || !decimal.TryParse(row.Multiplicador, NumberStyles.Number, CultureInfo.InvariantCulture, out var multiplicador))
            {
                continue;
            }

            var result = JerarquiaUoM.Create(productoId, unidadMayorId, unidadMenorId, multiplicador);
            if (result.IsSuccess)
            {
                candidates.Add(result.Value);
            }
        }

        var productoIds = candidates.Select(c => c.ProductoId).Distinct().ToList();
        var edgesByProducto = (await context.JerarquiasUoM
            .Where(x => productoIds.Contains(x.ProductoId))
            .Select(x => new { x.ProductoId, x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador })
            .ToListAsync(cancellationToken))
            .GroupBy(x => x.ProductoId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador)).ToList());

        var saved = 0;
        foreach (var candidate in candidates)
        {
            if (!edgesByProducto.TryGetValue(candidate.ProductoId, out var edges))
            {
                edges = [];
                edgesByProducto[candidate.ProductoId] = edges;
            }

            // Same rule as CreateJerarquiaUoM, extended to skip repeated edges and to see rows accepted earlier in the file
            var duplicada = edges.Exists(e => e.UnidadMayorId == candidate.UnidadMayorId && e.UnidadMenorId == candidate.UnidadMenorId);
            var yaResuelto = JerarquiaConversionResolver.Resolve(edges, candidate.UnidadMayorId, candidate.UnidadMenorId);
            if (duplicada || (yaResuelto.HasValue && decimal.Round(yaResuelto.Value, 4) != decimal.Round(candidate.Multiplicador, 4)))
            {
                continue;
            }

            edges.Add(new JerarquiaConversionResolver.Edge(candidate.UnidadMayorId, candidate.UnidadMenorId, candidate.Multiplicador));
            _ = context.JerarquiasUoM.Add(candidate);
            saved++;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success<int>(saved);
    }
}

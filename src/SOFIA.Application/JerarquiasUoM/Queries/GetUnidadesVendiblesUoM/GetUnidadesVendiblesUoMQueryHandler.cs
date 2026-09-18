using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetUnidadesVendiblesUoM;

public class GetUnidadesVendiblesUoMQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUnidadesVendiblesUoMQuery, Result<List<UnidadVendibleDto>>>
{
    public async Task<Result<List<UnidadVendibleDto>>> Handle(GetUnidadesVendiblesUoMQuery request, CancellationToken cancellationToken)
    {
        var producto = await context.Medicamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProductoId && !x.IsDeleted, cancellationToken);

        if (producto == null)
        {
            return Result.Failure<List<UnidadVendibleDto>>(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.ProductoId} was not found."), 404);
        }

        var edges = await context.JerarquiasUoM
            .AsNoTracking()
            .Where(x => x.ProductoId == request.ProductoId)
            .Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador))
            .ToListAsync(cancellationToken);

        // Sale units are every unit the product's Jerarquía connects to its base unit, other than the
        // base unit itself — selling "by the base unit" needs no presentación, it's the implicit default.
        var candidatos = edges
            .SelectMany(e => new[] { e.UnidadMayorId, e.UnidadMenorId })
            .Distinct()
            .Where(unidadId => unidadId != producto.UnidadBaseId);

        var resueltas = candidatos
            .Select(unidadId => (UnidadId: unidadId, Cantidad: JerarquiaConversionResolver.Resolve(edges, unidadId, producto.UnidadBaseId)))
            .Where(x => x.Cantidad.HasValue)
            .ToList();

        if (resueltas.Count == 0)
        {
            return Result.Success(new List<UnidadVendibleDto>());
        }

        var unidadIds = resueltas.Select(x => x.UnidadId).ToList();
        var nombres = await context.UnidadesMedida
            .AsNoTracking()
            .Where(x => unidadIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Descripcion, cancellationToken);

        var dtos = resueltas
            .Select(x => new UnidadVendibleDto(x.UnidadId, nombres.GetValueOrDefault(x.UnidadId, "?"), x.Cantidad!.Value))
            .OrderByDescending(x => x.CantidadUnidadesBase)
            .ToList();

        return Result.Success(dtos);
    }
}

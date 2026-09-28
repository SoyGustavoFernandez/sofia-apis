using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.SeriesFiscales.Queries.GetSeriesFiscales;

public class GetSeriesFiscalesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSeriesFiscalesQuery, Result<PaginatedList<SerieFiscalDto>>>
{
    public async Task<Result<PaginatedList<SerieFiscalDto>>> Handle(GetSeriesFiscalesQuery request, CancellationToken cancellationToken)
    {
        // Sucursal is required; a series whose branch was soft-deleted is invalid data and is excluded.
        var query = context.SUNATSeriesFiscales
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Sucursal != null);

        if (request.SucursalId.HasValue)
        {
            query = query.Where(s => s.SucursalId == request.SucursalId.Value);
        }

        if (request.TipoComprobante.HasValue)
        {
            query = query.Where(s => s.TipoComprobante == request.TipoComprobante.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EstadoSerie))
        {
            query = query.Where(s => s.EstadoSerie == request.EstadoSerie);
        }

        if (!string.IsNullOrWhiteSpace(request.PrefijoSerie))
        {
            var term = request.PrefijoSerie.Trim().ToUpper();
            query = query.Where(s => s.PrefijoSerie.Contains(term));
        }

        var page = await PaginatedList<SunatSerieFiscal>.CreateAsync(
            query.OrderBy(s => s.PrefijoSerie).ThenBy(s => s.TipoComprobante),
            request.PageNumber,
            request.PageSize);

        var sucursalIds = page.Items.Select(s => s.SucursalId).Distinct().ToList();
        var sucursales = await context.Sucursales
            .AsNoTracking()
            .Where(s => sucursalIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Nombre, cancellationToken);

        var serieIds = page.Items.Select(s => s.Id).ToList();
        var conComprobantes = await context.SUNATComprobantesEmitidos
            .AsNoTracking()
            .Where(c => serieIds.Contains(c.SerieId) && !c.IsDeleted)
            .Select(c => c.SerieId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var items = page.Items.Select(s => new SerieFiscalDto(
            s.Id,
            s.SucursalId,
            sucursales.GetValueOrDefault(s.SucursalId, string.Empty),
            s.TipoComprobante,
            s.PrefijoSerie,
            s.CorrelativoActual,
            s.EstadoSerie,
            conComprobantes.Contains(s.Id),
            s.CreatedAt)).ToList();

        return Result.Success(new PaginatedList<SerieFiscalDto>(items, page.TotalCount, page.PageNumber, request.PageSize));
    }
}

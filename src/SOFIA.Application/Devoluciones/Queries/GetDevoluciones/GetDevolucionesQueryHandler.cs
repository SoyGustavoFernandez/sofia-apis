using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Devoluciones.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public class GetDevolucionesQueryHandler(IApplicationDbContext dbContext, ISucursalAccess sucursalAccess) : IRequestHandler<GetDevolucionesQuery, Result<PaginatedList<DevolucionResumenDto>>>
{
    public async Task<Result<PaginatedList<DevolucionResumenDto>>> Handle(GetDevolucionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Devoluciones.AsNoTracking().AsQueryable();

        // A return belongs to the branch of the sale whose receipt it credits
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);
        query = query.WhereSucursalIn(d => dbContext.SUNATComprobantesEmitidos.Where(c => c.Id == d.ComprobanteOrigenId).Select(c => c.Transaccion!.SucursalId).FirstOrDefault(), allowed);

        if (request.EmpleadoAutorizaId.HasValue)
        {
            query = query.Where(d => d.EmpleadoAutorizaId == request.EmpleadoAutorizaId.Value);
        }

        query = query.WhereDateRange(d => d.FechaDevolucion, request.FechaInicio, request.FechaFin);

        if (request.MetodoReembolso.HasValue)
        {
            query = query.Where(d => d.MetodoReembolso == request.MetodoReembolso.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.MotivoSunatCatalogo))
        {
            var motivo = request.MotivoSunatCatalogo.Trim();
            query = query.Where(d => d.MotivoSunatCatalogo == motivo);
        }

        // The destination lives on each line, so a return matches when any of its lines went there
        if (request.Destino.HasValue)
        {
            var destino = request.Destino.Value;
            query = query.Where(d => dbContext.DetallesDevolucion.Any(l => l.DevolucionId == d.Id && !l.IsDeleted && l.DestinoFisicoLogico == destino));
        }

        if (request.MontoMin.HasValue)
        {
            query = query.Where(d => d.MontoReembolsado >= request.MontoMin.Value);
        }

        if (request.MontoMax.HasValue)
        {
            query = query.Where(d => d.MontoReembolsado <= request.MontoMax.Value);
        }

        query = query.WhereNumeroNotaCredito(dbContext, request.NumeroNotaCredito);

        var pagina = await PaginatedList<DevolucionCabecera>.CreateAsync(
            query.OrderByDescending(d => d.FechaDevolucion).ThenBy(d => d.Id),
            request.PageNumber,
            request.PageSize);

        var dtos = await dbContext.GetResumenesAsync([.. pagina.Items], cancellationToken);

        return Result.Success(new PaginatedList<DevolucionResumenDto>(dtos, pagina.TotalCount, pagina.PageNumber, request.PageSize));
    }
}

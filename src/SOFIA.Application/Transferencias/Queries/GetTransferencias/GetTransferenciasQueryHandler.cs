using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Transferencias.Queries.GetTransferencias;

public class GetTransferenciasQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetTransferenciasQuery, Result<PaginatedList<TransferenciaDto>>>
{
    public async Task<Result<PaginatedList<TransferenciaDto>>> Handle(GetTransferenciasQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<PaginatedList<TransferenciaDto>>(sucursalResult.Error);
        }

        var userSucursalId = sucursalResult.Value;

        // No collection Include here — mirrors GetVentasQueryHandler; navs/Detalles resolved separately below.
        var query = context.Transferencias
            .AsNoTracking()
            .Where(t => t.SucursalOrigenId == userSucursalId || t.SucursalDestinoId == userSucursalId);

        // Apply additional filters
        if (request.SucursalFiltroId.HasValue)
        {
            query = query.Where(t => t.SucursalOrigenId == request.SucursalFiltroId.Value || t.SucursalDestinoId == request.SucursalFiltroId.Value);
        }

        if (request.Estado.HasValue)
        {
            query = query.Where(t => t.EstadoLogistico == request.Estado.Value);
        }

        // 1. Fetch the paginated entity list (EF does the heavy lifting)
        var paginatedEntities = await PaginatedList<Transferencia>.CreateAsync(
            query.OrderByDescending(t => t.FechaDespacho),
            request.PageNumber,
            request.PageSize);

        var transferenciaIds = paginatedEntities.Items.Select(t => t.Id).ToList();

        // 2. Fetch related display fields via a Select projection (no Include, safe with the page's id list).
        var extras = await context.Transferencias
            .AsNoTracking()
            .Where(t => transferenciaIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                SucursalOrigenNombre = t.SucursalOrigen != null ? t.SucursalOrigen.Nombre : null,
                SucursalDestinoNombre = t.SucursalDestino != null ? t.SucursalDestino.Nombre : null,
                EmpleadoEmisorNombre = t.EmpleadoEmisor != null ? t.EmpleadoEmisor.Nombres + " " + t.EmpleadoEmisor.Apellido_Paterno : null,
                EmpleadoReceptorNombre = t.EmpleadoReceptor != null ? t.EmpleadoReceptor.Nombres + " " + t.EmpleadoReceptor.Apellido_Paterno : null,
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // 3. Fetch the Detalles collection for the page's transferencias separately (in-memory join, avoids the paginated-projection bug).
        var detallesPorTransferencia = await context.DetallesTransferencia
            .AsNoTracking()
            .Include(d => d.Lote)
            .Where(d => transferenciaIds.Contains(d.TransferenciaId) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var detallesLookup = detallesPorTransferencia
            .GroupBy(d => d.TransferenciaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Map to DTO in memory
        List<TransferenciaDto> dtos = [.. paginatedEntities.Items.Select(t =>
        {
            var extra = extras.GetValueOrDefault(t.Id);
            _ = detallesLookup.TryGetValue(t.Id, out var detalles);

            return new TransferenciaDto(
                t.Id,
                t.SucursalOrigenId,
                extra?.SucursalOrigenNombre ?? "Desconocida",
                t.SucursalDestinoId,
                extra?.SucursalDestinoNombre ?? "Desconocida",
                t.EstadoLogistico.ToString(),
                t.EmpleadoEmisorId,
                extra?.EmpleadoEmisorNombre ?? "Desconocido",
                t.EmpleadoReceptorId,
                extra?.EmpleadoReceptorNombre,
                t.FechaDespacho,
                t.FechaRecepcion,
                [.. (detalles ?? []).Select(d => new DetalleTransferenciaDto(
                    d.Id,
                    d.LoteId,
                    d.Lote?.NumeroLoteMfr ?? "Desconocido",
                    d.CantidadEnviada,
                    d.CantidadRecibida
                ))]);
        })];

        // 5. Return the new paginated list
        var result = new PaginatedList<TransferenciaDto>(
            dtos,
            paginatedEntities.TotalCount,
            paginatedEntities.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}

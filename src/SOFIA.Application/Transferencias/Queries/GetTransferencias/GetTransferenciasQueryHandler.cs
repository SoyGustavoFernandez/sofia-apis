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

        var query = context.Transferencias
            .AsNoTracking()
            .Include(t => t.SucursalOrigen)
            .Include(t => t.SucursalDestino)
            .Include(t => t.EmpleadoEmisor)
            .Include(t => t.EmpleadoReceptor)
            .Include(t => t.Detalles)
                .ThenInclude(d => d.Lote)
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

        var paginatedEntities = await PaginatedList<Transferencia>.CreateAsync(
            query.OrderByDescending(t => t.FechaDespacho),
            request.PageNumber,
            request.PageSize);

        List<TransferenciaDto> dtos = [.. paginatedEntities.Items.Select(t => new TransferenciaDto(
            t.Id,
            t.SucursalOrigenId,
            t.SucursalOrigen?.Nombre ?? "Desconocida",
            t.SucursalDestinoId,
            t.SucursalDestino?.Nombre ?? "Desconocida",
            t.EstadoLogistico.ToString(),
            t.EmpleadoEmisorId,
            t.EmpleadoEmisor != null ? $"{t.EmpleadoEmisor.Nombres} {t.EmpleadoEmisor.Apellido_Paterno}" : "Desconocido",
            t.EmpleadoReceptorId,
            t.EmpleadoReceptor != null ? $"{t.EmpleadoReceptor.Nombres} {t.EmpleadoReceptor.Apellido_Paterno}" : null,
            t.FechaDespacho,
            t.FechaRecepcion,
            [.. t.Detalles.Select(d => new DetalleTransferenciaDto(
                d.Id,
                d.LoteId,
                d.Lote?.NumeroLoteMfr ?? "Desconocido",
                d.CantidadEnviada,
                d.CantidadRecibida
            ))]
        ))];

        var result = new PaginatedList<TransferenciaDto>(
            dtos,
            paginatedEntities.TotalCount,
            paginatedEntities.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}

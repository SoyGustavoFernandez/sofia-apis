using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Onboarding.Queries.GetPrimerosPasos;

public class GetPrimerosPasosQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetPrimerosPasosQuery, Result<PrimerosPasosDto>>
{
    public async Task<Result<PrimerosPasosDto>> Handle(GetPrimerosPasosQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (!currentUser.IsInRole(Rol.AdminRoleName) || sucursalResult.IsFailure)
        {
            return Result.Success(PrimerosPasosDto.Oculto);
        }

        // A parked sale is not a first sale yet; a voided one already was
        if (await context.Ventas.AnyAsync(v => v.Estado != EstadoVenta.Pendiente && !v.IsDeleted, cancellationToken))
        {
            return Result.Success(PrimerosPasosDto.Oculto);
        }

        var sucursalId = sucursalResult.Value;

        var serieBoleta = await context.SUNATSeriesFiscales.AnyAsync(
            s => s.SucursalId == sucursalId && s.TipoComprobante == TipoComprobante.Boleta && s.EstadoSerie == SunatSerieFiscal.EstadoActiva && !s.IsDeleted,
            cancellationToken);

        var productoConStock = await HayProductoVendibleAsync(sucursalId, cancellationToken);
        var cajaAbierta = await CajaAbiertaAsync(sucursalId, cancellationToken);
        var equipoAgregado = await context.Empleados.CountAsync(e => !e.IsDeleted, cancellationToken) > 1;

        return Result.Success(new PrimerosPasosDto(true, sucursalId, serieBoleta, productoConStock, cajaAbierta, equipoAgregado));
    }

    // Sellable means priced, in stock at this branch and not expired
    private async Task<bool> HayProductoVendibleAsync(Guid sucursalId, CancellationToken cancellationToken)
    {
        var ultimaVencida = LoteInventario.UltimaCaducidadVencida(DateTimeOffset.UtcNow);

        return await (
            from i in context.LotesEnSucursal
            join l in context.LotesInventario on i.LoteId equals l.Id
            join m in context.Medicamentos on l.ProductoId equals m.Id
            where i.SucursalId == sucursalId && i.CantidadFisica > 0 && l.FechaCaducidad > ultimaVencida && m.PrecioVentaBase != null
            select i.Id)
            .AnyAsync(cancellationToken);
    }

    private async Task<bool> CajaAbiertaAsync(Guid sucursalId, CancellationToken cancellationToken)
    {
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return false;
        }

        var empleadoId = empleadoResult.Value;

        return await context.POSSesionesCaja.AnyAsync(
            s => s.SucursalId == sucursalId && s.EmpleadoId == empleadoId && s.EstadoSesion == EstadoSesion.Abierta && !s.IsDeleted,
            cancellationToken);
    }
}

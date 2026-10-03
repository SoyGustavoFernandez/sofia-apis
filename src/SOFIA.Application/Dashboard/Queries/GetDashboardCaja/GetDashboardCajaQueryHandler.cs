using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Application.POS.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardCaja;

public class GetDashboardCajaQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess, ICurrentUser currentUser)
    : IRequestHandler<GetDashboardCajaQuery, Result<DashboardCajaDto>>
{
    private const int MaximoSesionesAbiertas = 50;

    public async Task<Result<DashboardCajaDto>> Handle(GetDashboardCajaQuery request, CancellationToken cancellationToken)
    {
        var scope = await DashboardScopeResolver.ResolveAsync(context, sucursalAccess, cancellationToken);
        var ids = scope.Ids;

        var abiertas = await (
            from s in context.POSSesionesCaja.AsNoTracking()
            where ids.Contains(s.SucursalId) && s.EstadoSesion == EstadoSesion.Abierta
            join suc in context.Sucursales on s.SucursalId equals suc.Id into sucGroup
            from suc in sucGroup.DefaultIfEmpty()
            join emp in context.Empleados on s.EmpleadoId equals emp.Id into empGroup
            from emp in empGroup.DefaultIfEmpty()
            orderby s.FechaHoraApertura descending
            select new SesionAbiertaDto(
                s.Id,
                suc != null ? suc.Nombre : string.Empty,
                emp != null ? emp.Nombres + " " + emp.Apellido_Paterno : string.Empty,
                s.FechaHoraApertura,
                s.MontoAperturaEfectivo))
            .Take(MaximoSesionesAbiertas)
            .ToListAsync(cancellationToken);

        return Result.Success(new DashboardCajaDto(await GetMiSesionAsync(cancellationToken), abiertas));
    }

    // The expected cash runs several queries, so it is computed only for the caller's own drawer
    private async Task<MiSesionDto?> GetMiSesionAsync(CancellationToken cancellationToken)
    {
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return null;
        }

        var empleadoId = empleadoResult.Value;
        var sesion = await context.POSSesionesCaja
            .AsNoTracking()
            .Where(s => s.EmpleadoId == empleadoId && s.EstadoSesion == EstadoSesion.Abierta)
            .OrderByDescending(s => s.FechaHoraApertura)
            .FirstOrDefaultAsync(cancellationToken);

        if (sesion is null)
        {
            return null;
        }

        var sucursalNombre = await context.Sucursales
            .AsNoTracking()
            .Where(s => s.Id == sesion.SucursalId)
            .Select(s => s.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        var efectivoEsperado = await ArqueoCajaCalculator.CalcularEfectivoEsperadoAsync(context, sesion, cancellationToken);

        return new MiSesionDto(sesion.Id, sucursalNombre ?? string.Empty, sesion.FechaHoraApertura, sesion.MontoAperturaEfectivo, efectivoEsperado);
    }
}

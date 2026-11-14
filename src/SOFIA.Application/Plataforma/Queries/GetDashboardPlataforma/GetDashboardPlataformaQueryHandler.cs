using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Queries.GetDashboardPlataforma;

public class GetDashboardPlataformaQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetDashboardPlataformaQuery, Result<DashboardPlataformaDto>>
{
    // Same window as the notice companies see before their subscription expires
    public const int DiasPorVencer = 7;

    private const int MaxProximasAVencer = 10;

    public async Task<Result<DashboardPlataformaDto>> Handle(GetDashboardPlataformaQuery request, CancellationToken cancellationToken)
    {
        // Checked here too because this handler reads every company
        if (!currentUser.EsAdminPlataforma)
        {
            return Result.Failure<DashboardPlataformaDto>(PlataformaAdminPolicy.RequiredError, 403);
        }

        var ahora = DateTimeOffset.UtcNow;
        var limite = ahora.AddDays(DiasPorVencer);
        var inicioMes = request.InicioMes ?? new DateTimeOffset(ahora.Year, ahora.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var empresas = context.Empresas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AsNoTracking();

        // Mirrors Empresa.EstaVigenteEn in SQL: a trial or paid company counts as current only until its expiry
        var resumen = await empresas
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                EnPrueba = g.Count(e => e.Estado == EstadoEmpresa.TrialActivo && e.FechaVencimiento > ahora),
                ConSuscripcion = g.Count(e => e.Estado == EstadoEmpresa.Activo && e.FechaVencimiento > ahora),
                PorVencer = g.Count(e => (e.Estado == EstadoEmpresa.TrialActivo || e.Estado == EstadoEmpresa.Activo) && e.FechaVencimiento > ahora && e.FechaVencimiento <= limite),
                Vencidas = g.Count(e => (e.Estado == EstadoEmpresa.TrialActivo || e.Estado == EstadoEmpresa.Activo) && e.FechaVencimiento <= ahora),
                Suspendidas = g.Count(e => e.Estado == EstadoEmpresa.Suspendido),
                Canceladas = g.Count(e => e.Estado == EstadoEmpresa.Cancelado),
                RegistradasEsteMes = g.Count(e => e.CreatedAt >= inicioMes),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var proximas = await empresas
            .Where(e => (e.Estado == EstadoEmpresa.TrialActivo || e.Estado == EstadoEmpresa.Activo) && e.FechaVencimiento > ahora && e.FechaVencimiento <= limite)
            .OrderBy(e => e.FechaVencimiento)
            .ThenBy(e => e.Nombre)
            .Take(MaxProximasAVencer)
            .Select(e => new EmpresaPorVencerDto(e.Id, e.Nombre, e.Estado, e.FechaVencimiento))
            .ToListAsync(cancellationToken);

        return Result.Success(new DashboardPlataformaDto(
            resumen?.Total ?? 0,
            (resumen?.EnPrueba ?? 0) + (resumen?.ConSuscripcion ?? 0),
            resumen?.EnPrueba ?? 0,
            resumen?.ConSuscripcion ?? 0,
            resumen?.PorVencer ?? 0,
            resumen?.Vencidas ?? 0,
            resumen?.Suspendidas ?? 0,
            resumen?.Canceladas ?? 0,
            resumen?.RegistradasEsteMes ?? 0,
            DiasPorVencer,
            proximas));
    }
}

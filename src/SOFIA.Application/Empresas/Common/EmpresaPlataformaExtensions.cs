using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Common;

public static class EmpresaPlataformaExtensions
{
    public static readonly Error PropiaEmpresaError =
        Error.Conflict("Empresa.Suscripcion.PropiaEmpresa", "The platform operator cannot block its own company.");

    // The platform operator works across companies; everyone else stays inside their own tenant
    public static IQueryable<Empresa> EmpresasVisibles(this IApplicationDbContext context, ICurrentUser currentUser) =>
        currentUser.EsAdminPlataforma
            ? context.Empresas.IgnoreQueryFilters([QueryFilters.Tenant])
            : context.Empresas;

    public static bool EsSuPropiaEmpresa(this ICurrentUser currentUser, Guid empresaId) =>
        Guid.TryParse(currentUser.EmpresaId, out var propia) && propia == empresaId;
}

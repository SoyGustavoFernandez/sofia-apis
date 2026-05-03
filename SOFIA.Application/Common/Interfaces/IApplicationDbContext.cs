using Microsoft.EntityFrameworkCore;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Sucursal> Sucursales { get; }
    DbSet<Empleado> Empleados { get; }
    DbSet<UnidadMedida> UnidadesMedida { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

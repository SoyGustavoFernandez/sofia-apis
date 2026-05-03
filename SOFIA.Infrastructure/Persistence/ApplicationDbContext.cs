using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using System.Reflection;

namespace SOFIA.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<IngredienteActivo> IngredientesActivos => Set<IngredienteActivo>();
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
    public DbSet<JerarquiaUoM> JerarquiasUoM => Set<JerarquiaUoM>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUser.Id;
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = currentUser.Id;
                    entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedBy = currentUser.Id;
                    entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsDeleted = true;
                    break;
                case EntityState.Detached:
                    break;
                case EntityState.Unchanged:
                    break;
                default:
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

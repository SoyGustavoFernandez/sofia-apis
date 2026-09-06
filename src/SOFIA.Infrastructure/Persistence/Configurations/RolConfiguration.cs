using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        _ = builder.ToTable("Seguridad_Roles");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Rol_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.NombreRol)
            .HasColumnName("Nombre_Rol")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.Descripcion)
            .HasColumnName("Descripcion")
            .HasMaxLength(255);

        _ = builder.Property(x => x.NivelJerarquia)
            .HasColumnName("Nivel_Jerarquia")
            .IsRequired();

        _ = builder.HasQueryFilter(x => !x.IsDeleted);

        _ = builder.HasMany(x => x.Sucursales)
            .WithMany()
            .UsingEntity<RolSucursal>(
                l => l.HasOne(rs => rs.Sucursal).WithMany().HasForeignKey(rs => rs.SucursalId),
                r => r.HasOne(rs => rs.Rol).WithMany().HasForeignKey(rs => rs.RolId),
                j =>
                {
                    j.ToTable("Seguridad_Roles_Sucursales");
                    j.HasKey(rs => new { rs.RolId, rs.SucursalId });
                    j.Property(rs => rs.RolId).HasColumnName("Rol_ID");
                    j.Property(rs => rs.SucursalId).HasColumnName("Sucursal_ID");
                });
    }
}

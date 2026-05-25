using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class PermisoRolConfiguration : IEntityTypeConfiguration<PermisoRol>
{
    public void Configure(EntityTypeBuilder<PermisoRol> builder)
    {
        _ = builder.ToTable("Seguridad_Permisos_Rol");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Permiso_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.RolId)
            .HasColumnName("Rol_ID")
            .IsRequired();

        _ = builder.Property(x => x.ModuloSistema)
            .HasColumnName("Modulo_Sistema")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.Accion)
            .HasColumnName("Accion")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.HasOne(x => x.Rol)
            .WithMany(r => r.Permisos)
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasIndex(x => new { x.RolId, x.ModuloSistema, x.Accion })
            .IsUnique()
            .HasDatabaseName("UQ_Rol_Modulo_Accion");
    }
}

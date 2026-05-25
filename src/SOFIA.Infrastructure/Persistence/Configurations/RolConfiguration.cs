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
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        _ = builder.ToTable("Empleados");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Empleado_ID")
            .ValueGeneratedOnAdd(); // DB generates NEWID()

        _ = builder.Property(x => x.Nombres)
            .HasColumnName("Nombres")
            .HasMaxLength(75)
            .IsRequired();

        _ = builder.Property(x => x.Apellido_Paterno)
            .HasColumnName("Apellido_Paterno")
            .HasMaxLength(75)
            .IsRequired();

        _ = builder.Property(x => x.Apellido_Materno)
            .HasColumnName("Apellido_Materno")
            .HasMaxLength(75)
            .IsRequired();

        _ = builder.Ignore(x => x.Nombre_Completo);

        _ = builder.Property(x => x.Licencia_Prof)
            .HasColumnName("Licencia_Prof")
            .HasMaxLength(50);

        _ = builder.Property(x => x.Huella_Biometrica)
            .HasColumnName("Huella_Biometrica");

        _ = builder.HasOne(x => x.Sucursal_Base)
            .WithMany(x => x.Empleados)
            .HasForeignKey(x => x.Sucursal_Base_ID)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class ProfesionalSaludConfiguration : IEntityTypeConfiguration<ProfesionalSalud>
{
    public void Configure(EntityTypeBuilder<ProfesionalSalud> builder)
    {
        _ = builder.ToTable("Profesionales_Salud");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Medico_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.NumeroRegistro)
            .HasColumnName("Numero_Registro")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.NombrePrescriptor)
            .HasColumnName("Nombre_Prescriptor")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.DireccionClinica)
            .HasColumnName("Direccion_Clinica")
            .HasMaxLength(255);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Indexes
        _ = builder.HasIndex(x => x.NumeroRegistro)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

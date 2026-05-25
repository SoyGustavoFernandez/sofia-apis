using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class AseguradoraMedicaConfiguration : IEntityTypeConfiguration<AseguradoraMedica>
{
    public void Configure(EntityTypeBuilder<AseguradoraMedica> builder)
    {
        _ = builder.ToTable("Aseguradoras_Medicas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Aseguradora_ID");

        _ = builder.Property(x => x.NombreComercial)
            .HasColumnName("Nombre_Comercial")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.CodigoIdentificadorNacional)
            .HasColumnName("Codigo_Identificador_Nacional")
            .HasMaxLength(50)
            .IsRequired();

        // Audit & Soft Delete
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

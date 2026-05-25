using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class ProveedorDistribuidorConfiguration : IEntityTypeConfiguration<ProveedorDistribuidor>
{
    public void Configure(EntityTypeBuilder<ProveedorDistribuidor> builder)
    {
        _ = builder.ToTable("Proveedores_Dist");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Proveedor_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.RazonSocial)
            .HasColumnName("Razon_Social")
            .HasMaxLength(200)
            .IsRequired();

        _ = builder.Property(x => x.TaxId)
            .HasColumnName("Tax_ID")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.TerminosFinancieros)
            .HasColumnName("Terminos_Financieros")
            .HasMaxLength(100);

        _ = builder.Property(x => x.CalificacionEsg)
            .HasColumnName("Calificacion_ESG")
            .HasPrecision(5, 2);

        _ = builder.Property(x => x.TasaCumplimiento)
            .HasColumnName("Tasa_Cumplimiento")
            .HasPrecision(5, 2)
            .IsRequired();

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Indexes
        _ = builder.HasIndex(x => x.TaxId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

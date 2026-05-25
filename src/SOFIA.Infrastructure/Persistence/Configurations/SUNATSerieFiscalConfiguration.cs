using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class SUNATSerieFiscalConfiguration : IEntityTypeConfiguration<SUNATSerieFiscal>
{
    public void Configure(EntityTypeBuilder<SUNATSerieFiscal> builder)
    {
        _ = builder.ToTable("SUNAT_Series_Fiscales");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Serie_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID")
            .IsRequired();

        _ = builder.Property(x => x.TipoComprobante)
            .HasConversion(new EnumDescriptionConverter<TipoComprobante>())
            .HasColumnName("Tipo_Comprobante")
            .HasMaxLength(2)
            .IsRequired();

        _ = builder.Property(x => x.PrefijoSerie)
            .HasColumnName("Prefijo_Serie")
            .HasMaxLength(4)
            .IsRequired();

        _ = builder.Property(x => x.CorrelativoActual)
            .HasColumnName("Correlativo_Actual")
            .IsRequired();

        _ = builder.Property(x => x.EstadoSerie)
            .HasColumnName("Estado_Serie")
            .HasMaxLength(10)
            .IsRequired();

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

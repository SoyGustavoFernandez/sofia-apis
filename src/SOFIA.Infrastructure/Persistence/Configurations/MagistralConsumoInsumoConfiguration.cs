using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class MagistralConsumoInsumoConfiguration : IEntityTypeConfiguration<MagistralConsumoInsumo>
{
    public void Configure(EntityTypeBuilder<MagistralConsumoInsumo> builder)
    {
        _ = builder.ToTable("Magistrales_Consumo_Insumos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Consumo_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.OrdenProduccionId)
            .HasColumnName("Orden_Produccion_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteMateriaPrimaId)
            .HasColumnName("Lote_Materia_Prima_ID")
            .IsRequired();

        _ = builder.Property(x => x.CantidadConsumida)
            .HasColumnName("Cantidad_Consumida")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.UnidadMedida)
            .HasColumnName("Unidad_Medida")
            .HasMaxLength(20)
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
        _ = builder.HasOne(x => x.OrdenProduccion)
            .WithMany(o => o.Consumos)
            .HasForeignKey(x => x.OrdenProduccionId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.LoteMateriaPrima)
            .WithMany()
            .HasForeignKey(x => x.LoteMateriaPrimaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class MagistralOrdenProduccionConfiguration : IEntityTypeConfiguration<MagistralOrdenProduccion>
{
    public void Configure(EntityTypeBuilder<MagistralOrdenProduccion> builder)
    {
        _ = builder.ToTable("Magistrales_Ordenes_Produccion");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Orden_Produccion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID")
            .IsRequired();

        _ = builder.Property(x => x.RecetaId)
            .HasColumnName("Receta_ID");

        _ = builder.Property(x => x.ProductoResultanteId)
            .HasColumnName("Producto_Resultante_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteGeneradoId)
            .HasColumnName("Lote_Generado_ID");

        _ = builder.Property(x => x.CantidadProducida)
            .HasColumnName("Cantidad_Producida")
            .HasPrecision(12, 4);

        _ = builder.Property(x => x.QuimicoPreparadorId)
            .HasColumnName("Quimico_Preparador_ID")
            .IsRequired();

        _ = builder.Property(x => x.FechaPreparacion)
            .HasColumnName("Fecha_Preparacion")
            .IsRequired();

        _ = builder.Property(x => x.EstadoProduccion)
            .HasColumnName("Estado_Produccion")
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
        _ = builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.ProductoResultante)
            .WithMany()
            .HasForeignKey(x => x.ProductoResultanteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.LoteGenerado)
            .WithMany()
            .HasForeignKey(x => x.LoteGeneradoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.QuimicoPreparador)
            .WithMany()
            .HasForeignKey(x => x.QuimicoPreparadorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasMany(x => x.Consumos)
            .WithOne(c => c.OrdenProduccion)
            .HasForeignKey(c => c.OrdenProduccionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

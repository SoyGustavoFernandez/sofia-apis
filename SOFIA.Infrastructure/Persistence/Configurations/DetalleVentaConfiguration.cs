using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        _ = builder.ToTable("Ventas_Detalle");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Detalle_ID");

        _ = builder.Property(x => x.VentaId)
            .HasColumnName("Transaccion_ID");

        _ = builder.Property(x => x.LoteId)
            .HasColumnName("Lote_ID");

        _ = builder.Property(x => x.RecetaId)
            .HasColumnName("Receta_ID");

        _ = builder.Property(x => x.CantidadVendida)
            .HasColumnName("Cantidad_Vendida")
            .HasPrecision(8, 2);

        _ = builder.Property(x => x.PrecioFijadoUnidad)
            .HasColumnName("Precio_Fijado_Unidad")
            .HasPrecision(10, 2);

        _ = builder.Property(x => x.CostoUnitarioHistorico)
            .HasColumnName("Costo_Unitario_Historico")
            .HasPrecision(10, 2);

        _ = builder.HasOne(x => x.Venta)
            .WithMany(v => v.Detalles)
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.Lote)
            .WithMany()
            .HasForeignKey(x => x.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

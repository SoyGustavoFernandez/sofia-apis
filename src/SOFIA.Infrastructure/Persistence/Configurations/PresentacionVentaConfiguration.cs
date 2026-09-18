using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class PresentacionVentaConfiguration : IEntityTypeConfiguration<PresentacionVenta>
{
    public void Configure(EntityTypeBuilder<PresentacionVenta> builder)
    {
        _ = builder.ToTable("Presentaciones_Venta");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Presentacion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.UnidadVentaId)
            .HasColumnName("Unidad_Venta_ID")
            .IsRequired();

        _ = builder.Property(x => x.Descripcion)
            .HasColumnName("Descripcion")
            .HasMaxLength(100)
            .IsRequired();

        _ = builder.Property(x => x.CantidadUnidadesBase)
            .HasColumnName("Cantidad_Unidades_Base")
            .HasColumnType("DECIMAL(12,4)")
            .IsRequired();

        _ = builder.Property(x => x.PrecioVenta)
            .HasColumnName("Precio_Venta")
            .HasColumnType("DECIMAL(10,2)")
            .IsRequired();

        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.UnidadVenta)
            .WithMany()
            .HasForeignKey(x => x.UnidadVentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

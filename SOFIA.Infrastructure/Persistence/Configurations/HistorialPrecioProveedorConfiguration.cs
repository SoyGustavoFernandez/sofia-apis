using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class HistorialPrecioProveedorConfiguration : IEntityTypeConfiguration<HistorialPrecioProveedor>
{
    public void Configure(EntityTypeBuilder<HistorialPrecioProveedor> builder)
    {
        _ = builder.ToTable("Historial_Precios_Prov");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Cotizacion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProveedorId)
            .HasColumnName("Proveedor_ID")
            .IsRequired();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.CostoPorUnidadBase)
            .HasColumnName("Costo_Por_Unidad_Base")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.FechaInicioVigencia)
            .HasColumnName("Fecha_Inicio_Vigencia")
            .IsRequired();

        _ = builder.Property(x => x.FechaFinVigencia)
            .HasColumnName("Fecha_Fin_Vigencia");

        _ = builder.Property(x => x.LeadTimeDias)
            .HasColumnName("Lead_Time_Dias")
            .IsRequired();

        _ = builder.Property(x => x.CantidadMinCompra)
            .HasColumnName("Cantidad_Min_Compra")
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
        _ = builder.HasOne(x => x.Proveedor)
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        _ = builder.HasIndex(x => x.ProveedorId);
        _ = builder.HasIndex(x => x.ProductoId);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class DevolucionDetalleConfiguration : IEntityTypeConfiguration<DevolucionDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionDetalle> builder)
    {
        _ = builder.ToTable("Devoluciones_Detalle");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Detalle_Dev_ID");

        _ = builder.Property(x => x.DevolucionId)
            .HasColumnName("Devolucion_ID")
            .IsRequired();

        _ = builder.Property(x => x.DetalleVentaId)
            .HasColumnName("Detalle_Venta_ID")
            .IsRequired();

        _ = builder.Property(x => x.CantidadDevuelta)
            .HasColumnName("Cantidad_Devuelta")
            .HasPrecision(8, 2)
            .IsRequired();

        _ = builder.Property(x => x.DestinoFisicoLogico)
            .HasConversion(new EnumDescriptionConverter<DestinoDevolucion>())
            .HasColumnName("Destino_Fisico_Logico")
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

        // Relationships
        _ = builder.HasOne(x => x.Devolucion)
            .WithMany(dc => dc.Detalles)
            .HasForeignKey(x => x.DevolucionId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.DetalleVenta)
            .WithMany()
            .HasForeignKey(x => x.DetalleVentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

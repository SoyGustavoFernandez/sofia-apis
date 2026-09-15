using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class VentaPagoConfiguration : IEntityTypeConfiguration<VentaPago>
{
    public void Configure(EntityTypeBuilder<VentaPago> builder)
    {
        _ = builder.ToTable("Ventas_Pagos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Pago_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.TransaccionId)
            .HasColumnName("Transaccion_ID")
            .IsRequired();

        _ = builder.Property(x => x.MetodoPago)
            .HasConversion(new EnumDescriptionConverter<MetodoPago>())
            .HasColumnName("Metodo_Pago")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.MontoPagado)
            .HasColumnName("Monto_Pagado")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.ReferenciaOperacion)
            .HasColumnName("Referencia_Operacion")
            .HasMaxLength(100);

        _ = builder.Property(x => x.FechaPago)
            .HasColumnName("Fecha_Pago")
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
        _ = builder.HasOne(x => x.Venta)
            .WithMany(v => v.Pagos)
            .HasForeignKey(x => x.TransaccionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        _ = builder.HasIndex(x => x.TransaccionId);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

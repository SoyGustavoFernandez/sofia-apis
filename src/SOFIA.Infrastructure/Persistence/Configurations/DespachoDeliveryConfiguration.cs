using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class DespachoDeliveryConfiguration : IEntityTypeConfiguration<DespachoDelivery>
{
    public void Configure(EntityTypeBuilder<DespachoDelivery> builder)
    {
        _ = builder.ToTable("Despachos_Delivery");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Despacho_ID");

        _ = builder.Property(x => x.VentaId)
            .HasColumnName("Transaccion_ID")
            .IsRequired();

        _ = builder.Property(x => x.PlataformaServicio)
            .HasColumnName("Plataforma_Servicio")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.CodigoRastreo)
            .HasColumnName("Codigo_Rastreo")
            .HasMaxLength(100);

        _ = builder.Property(x => x.EstadoDespacho)
            .HasConversion(new EnumDescriptionConverter<EstadoDespacho>())
            .HasColumnName("Estado_Despacho")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.DireccionEntrega)
            .HasColumnName("Direccion_Entrega")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.RepartidorNombre)
            .HasColumnName("Repartidor_Nombre")
            .HasMaxLength(150);

        _ = builder.Property(x => x.EvidenciaFotograficaUrl)
            .HasColumnName("Evidencia_Fotografica_URL")
            .HasMaxLength(500);

        // Audit & Soft Delete
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Venta)
            .WithMany()
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

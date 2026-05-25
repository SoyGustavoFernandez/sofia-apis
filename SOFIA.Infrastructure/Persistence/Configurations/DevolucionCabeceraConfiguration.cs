using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class DevolucionCabeceraConfiguration : IEntityTypeConfiguration<DevolucionCabecera>
{
    public void Configure(EntityTypeBuilder<DevolucionCabecera> builder)
    {
        _ = builder.ToTable("Devoluciones_Cabecera");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Devolucion_ID");

        _ = builder.Property(x => x.ComprobanteOrigenId)
            .HasColumnName("Comprobante_Origen_ID")
            .IsRequired();

        _ = builder.Property(x => x.ComprobanteNcId)
            .HasColumnName("Comprobante_NC_ID");

        _ = builder.Property(x => x.EmpleadoAutorizaId)
            .HasColumnName("Empleado_Autoriza_ID")
            .IsRequired();

        _ = builder.Property(x => x.MotivoSunatCatalogo)
            .HasColumnName("Motivo_SUNAT_Catalogo")
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        _ = builder.Property(x => x.SustentoDescriptivo)
            .HasColumnName("Sustento_Descriptivo")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.FechaDevolucion)
            .HasColumnName("Fecha_Devolucion")
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
        _ = builder.HasOne(x => x.EmpleadoAutoriza)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoAutorizaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasMany(x => x.Detalles)
            .WithOne(d => d.Devolucion)
            .HasForeignKey(d => d.DevolucionId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

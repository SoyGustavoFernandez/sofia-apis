using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class SistemaNotificacionInternaConfiguration : IEntityTypeConfiguration<SistemaNotificacionInterna>
{
    public void Configure(EntityTypeBuilder<SistemaNotificacionInterna> builder)
    {
        _ = builder.ToTable("Sistema_Notificaciones_Internas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Notificacion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.EmpleadoOrigenId)
            .HasColumnName("Empleado_Origen_ID")
            .IsRequired();

        _ = builder.Property(x => x.EmpleadoDestinoId)
            .HasColumnName("Empleado_Destino_ID");

        _ = builder.Property(x => x.SucursalDestinoId)
            .HasColumnName("Sucursal_Destino_ID")
            .IsRequired();

        _ = builder.Property(x => x.MensajeTexto)
            .HasColumnName("Mensaje_Texto")
            .HasMaxLength(500)
            .IsRequired();

        _ = builder.Property(x => x.EntidadRelacionada)
            .HasColumnName("Entidad_Relacionada")
            .HasMaxLength(50);

        _ = builder.Property(x => x.EntidadId)
            .HasColumnName("Entidad_ID");

        _ = builder.Property(x => x.Leido)
            .HasColumnName("Leido")
            .IsRequired();

        _ = builder.Property(x => x.FechaLectura)
            .HasColumnName("Fecha_Lectura");

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.EmpleadoOrigen)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.EmpleadoDestino)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.SucursalDestino)
            .WithMany()
            .HasForeignKey(x => x.SucursalDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

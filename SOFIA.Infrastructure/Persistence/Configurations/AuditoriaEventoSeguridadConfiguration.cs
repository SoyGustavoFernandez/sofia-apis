using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class AuditoriaEventoSeguridadConfiguration : IEntityTypeConfiguration<AuditoriaEventoSeguridad>
{
    public void Configure(EntityTypeBuilder<AuditoriaEventoSeguridad> builder)
    {
        _ = builder.ToTable("Auditoria_Eventos_Seguridad");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Evento_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.EmpleadoId)
            .HasColumnName("Empleado_ID")
            .IsRequired();

        _ = builder.Property(x => x.TablaAfectada)
            .HasColumnName("Tabla_Afectada")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.RegistroIdAfectado)
            .HasColumnName("Registro_ID_Afectado")
            .IsRequired();

        _ = builder.Property(x => x.TipoAccion)
            .HasColumnName("Tipo_Accion")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.Property(x => x.PayloadAnterior)
            .HasColumnName("Payload_Anterior");

        _ = builder.Property(x => x.PayloadNuevo)
            .HasColumnName("Payload_Nuevo");

        _ = builder.Property(x => x.FechaHoraEvento)
            .HasColumnName("Fecha_Hora_Evento")
            .IsRequired();

        _ = builder.Property(x => x.DireccionIp)
            .HasColumnName("Direccion_IP")
            .HasMaxLength(45);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Empleado)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

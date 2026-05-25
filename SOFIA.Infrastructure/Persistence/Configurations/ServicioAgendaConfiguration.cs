using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class ServicioAgendaConfiguration : IEntityTypeConfiguration<ServicioAgenda>
{
    public void Configure(EntityTypeBuilder<ServicioAgenda> builder)
    {
        _ = builder.ToTable("Servicios_Agenda");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Agenda_ID");

        _ = builder.Property(x => x.ClienteId)
            .HasColumnName("Cliente_ID")
            .IsRequired();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.VentaId)
            .HasColumnName("Venta_ID");

        _ = builder.Property(x => x.FechaHoraProgramada)
            .HasColumnName("Fecha_Hora_Programada")
            .IsRequired();

        _ = builder.Property(x => x.EstadoCita)
            .HasColumnName("Estado_Cita")
            .HasMaxLength(20)
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
        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Venta)
            .WithMany()
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

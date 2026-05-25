using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class VentaReclamoSeguroConfiguration : IEntityTypeConfiguration<VentaReclamoSeguro>
{
    public void Configure(EntityTypeBuilder<VentaReclamoSeguro> builder)
    {
        _ = builder.ToTable("Ventas_Reclamos_Seguro");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Reclamo_ID");

        _ = builder.Property(x => x.DetalleVentaId)
            .HasColumnName("Detalle_ID")
            .IsRequired();

        _ = builder.Property(x => x.AseguradoraId)
            .HasColumnName("Aseguradora_ID")
            .IsRequired();

        _ = builder.Property(x => x.MontoCubierto)
            .HasColumnName("Monto_Cubierto")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.MontoCopagoPaciente)
            .HasColumnName("Monto_Copago_Paciente")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.EstadoReclamo)
            .HasColumnName("Estado_Reclamo")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.CodigoAutorizacion)
            .HasColumnName("Codigo_Autorizacion")
            .HasMaxLength(100);

        // Audit & Soft Delete
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.DetalleVenta)
            .WithMany()
            .HasForeignKey(x => x.DetalleVentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Aseguradora)
            .WithMany(a => a.Reclamos)
            .HasForeignKey(x => x.AseguradoraId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

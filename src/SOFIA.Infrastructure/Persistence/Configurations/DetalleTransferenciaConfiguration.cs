using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class DetalleTransferenciaConfiguration : IEntityTypeConfiguration<DetalleTransferencia>
{
    public void Configure(EntityTypeBuilder<DetalleTransferencia> builder)
    {
        _ = builder.ToTable("Transferencias_Det");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Detalle_Transf_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.TransferenciaId)
            .HasColumnName("Transferencia_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteId)
            .HasColumnName("Lote_ID")
            .IsRequired();

        _ = builder.Property(x => x.CantidadEnviada)
            .HasColumnName("Cantidad_Enviada")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.CantidadRecibida)
            .HasColumnName("Cantidad_Recibida")
            .HasPrecision(12, 4);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Transferencia)
            .WithMany(t => t.Detalles)
            .HasForeignKey(x => x.TransferenciaId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.Lote)
            .WithMany()
            .HasForeignKey(x => x.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

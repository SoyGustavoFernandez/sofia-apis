using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class LoteInventarioConfiguration : IEntityTypeConfiguration<LoteInventario>
{
    public void Configure(EntityTypeBuilder<LoteInventario> builder)
    {
        _ = builder.ToTable("Lotes_Inventario");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Lote_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.NumeroLoteMfr)
            .HasColumnName("Numero_Lote_Mfr")
            .HasMaxLength(100)
            .IsRequired();

        _ = builder.Property(x => x.FechaFabricacion)
            .HasColumnName("Fecha_Fabricacion");

        _ = builder.Property(x => x.FechaCaducidad)
            .HasColumnName("Fecha_Caducidad")
            .IsRequired();

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasIndex(x => x.FechaCaducidad);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

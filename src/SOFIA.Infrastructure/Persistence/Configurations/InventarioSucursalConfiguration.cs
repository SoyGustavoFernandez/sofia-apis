using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class InventarioSucursalConfiguration : IEntityTypeConfiguration<InventarioSucursal>
{
    public void Configure(EntityTypeBuilder<InventarioSucursal> builder)
    {
        _ = builder.ToTable("Inventario_Sucursal");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Registro_Inv_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteId)
            .HasColumnName("Lote_ID")
            .IsRequired();

        _ = builder.Property(x => x.CantidadFisica)
            .HasColumnName("Cantidad_Fisica")
            .HasColumnType("DECIMAL(12,4)")
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
        _ = builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Lote)
            .WithMany()
            .HasForeignKey(x => x.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for the combination of Sucursal and Batch
        _ = builder.HasIndex(x => new { x.SucursalId, x.LoteId })
            .HasDatabaseName("UX_Inventario_Sucursal_Lote")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

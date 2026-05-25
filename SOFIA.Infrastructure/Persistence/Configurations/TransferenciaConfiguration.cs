using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        _ = builder.ToTable("Transferencias_Cab");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Transferencia_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalOrigenId)
            .HasColumnName("Sucursal_Origen")
            .IsRequired();

        _ = builder.Property(x => x.SucursalDestinoId)
            .HasColumnName("Sucursal_Destino")
            .IsRequired();

        _ = builder.Property(x => x.EstadoLogistico)
            .HasColumnName("Estado_Logistico")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        _ = builder.Property(x => x.EmpleadoEmisorId)
            .HasColumnName("Empleado_Emisor")
            .IsRequired();

        _ = builder.Property(x => x.EmpleadoReceptorId)
            .HasColumnName("Empleado_Receptor");

        _ = builder.Property(x => x.FechaDespacho)
            .HasColumnName("Fecha_Despacho")
            .IsRequired();

        _ = builder.Property(x => x.FechaRecepcion)
            .HasColumnName("Fecha_Recepcion");

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.SucursalOrigen)
            .WithMany()
            .HasForeignKey(x => x.SucursalOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.SucursalDestino)
            .WithMany()
            .HasForeignKey(x => x.SucursalDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.EmpleadoEmisor)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoEmisorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.EmpleadoReceptor)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoReceptorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasMany(x => x.Detalles)
            .WithOne(x => x.Transferencia)
            .HasForeignKey(x => x.TransferenciaId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

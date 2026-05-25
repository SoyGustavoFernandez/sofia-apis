using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class DIGEMIDInventarioCuarentenaConfiguration : IEntityTypeConfiguration<DIGEMIDInventarioCuarentena>
{
    public void Configure(EntityTypeBuilder<DIGEMIDInventarioCuarentena> builder)
    {
        _ = builder.ToTable("DIGEMID_Inventario_Cuarentena");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Registro_Cuarentena_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteId)
            .HasColumnName("Lote_ID")
            .IsRequired();

        _ = builder.Property(x => x.DetalleDevId)
            .HasColumnName("Detalle_Dev_ID");

        _ = builder.Property(x => x.CantidadAislada)
            .HasColumnName("Cantidad_Aislada")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.MotivoAislamiento)
            .HasColumnName("Motivo_Aislamiento")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.FechaIngresoCuarentena)
            .HasColumnName("Fecha_Ingreso_Cuarentena")
            .IsRequired();

        _ = builder.Property(x => x.EstadoResolucion)
            .HasColumnName("Estado_Resolucion")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.Property(x => x.EmpleadoRegistraId)
            .HasColumnName("Empleado_Registra_ID")
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

        _ = builder.HasOne(x => x.EmpleadoRegistra)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoRegistraId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

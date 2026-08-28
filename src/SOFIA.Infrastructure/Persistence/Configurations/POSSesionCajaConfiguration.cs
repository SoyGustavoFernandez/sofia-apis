using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class PosSesionCajaConfiguration : IEntityTypeConfiguration<PosSesionCaja>
{
    public void Configure(EntityTypeBuilder<PosSesionCaja> builder)
    {
        _ = builder.ToTable("POS_Sesiones_Caja");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Sesion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID")
            .IsRequired();

        _ = builder.Property(x => x.EmpleadoId)
            .HasColumnName("Empleado_ID")
            .IsRequired();

        _ = builder.Property(x => x.FechaHoraApertura)
            .HasColumnName("Fecha_Hora_Apertura")
            .IsRequired();

        _ = builder.Property(x => x.FechaHoraCierre)
            .HasColumnName("Fecha_Hora_Cierre");

        _ = builder.Property(x => x.MontoAperturaEfectivo)
            .HasColumnName("Monto_Apertura_Efectivo")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.MontoCierreCalculado)
            .HasColumnName("Monto_Cierre_Calculado")
            .HasPrecision(12, 2);

        _ = builder.Property(x => x.MontoCierreDeclarado)
            .HasColumnName("Monto_Cierre_Declarado")
            .HasPrecision(12, 2);

        _ = builder.Property(x => x.DiferenciaArqueo)
            .HasColumnName("Diferencia_Arqueo")
            .HasPrecision(12, 2);

        _ = builder.Property(x => x.EstadoSesion)
            .HasConversion(new EnumDescriptionConverter<EstadoSesion>())
            .HasColumnName("Estado_Sesion")
            .HasMaxLength(20)
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

        _ = builder.HasOne(x => x.Empleado)
            .WithMany()
            .HasForeignKey(x => x.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        _ = builder.HasIndex(x => x.SucursalId);
        _ = builder.HasIndex(x => x.EmpleadoId);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

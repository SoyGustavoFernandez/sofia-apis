using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        _ = builder.ToTable("Ventas_Cabecera");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Transaccion_ID");

        _ = builder.Property(x => x.SucursalId)
            .HasColumnName("Sucursal_ID");

        _ = builder.Property(x => x.EmpleadoId)
            .HasColumnName("Empleado_ID");

        _ = builder.Property(x => x.ClienteId)
            .HasColumnName("Cliente_ID");

        _ = builder.Property(x => x.SesionId)
            .HasColumnName("Sesion_ID");

        _ = builder.Property(x => x.FechaHoraUtc)
            .HasColumnName("Fecha_Hora_UTC")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        _ = builder.Property(x => x.MontoTotalBruto)
            .HasColumnName("Monto_Total_Bruto")
            .HasPrecision(12, 2);

        _ = builder.Property(x => x.Estado)
            .HasConversion(new EnumDescriptionConverter<EstadoVenta>())
            .HasColumnName("Estado")
            .HasConversion<string>()
            .HasMaxLength(20);

        _ = builder.Property(x => x.MotivoAnulacion)
            .HasColumnName("Motivo_Anulacion")
            .HasMaxLength(255);

        _ = builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

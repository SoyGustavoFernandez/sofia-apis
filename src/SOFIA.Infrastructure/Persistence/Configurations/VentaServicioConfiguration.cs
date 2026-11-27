using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class VentaServicioConfiguration : IEntityTypeConfiguration<VentaServicio>
{
    public void Configure(EntityTypeBuilder<VentaServicio> builder)
    {
        _ = builder.ToTable("Ventas_Servicios");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("VentaServicio_ID");

        _ = builder.Property(x => x.VentaId).HasColumnName("Transaccion_ID");
        _ = builder.Property(x => x.ServicioId).HasColumnName("Servicio_ID");
        _ = builder.Property(x => x.CitaId).HasColumnName("Cita_ID");
        _ = builder.Property(x => x.Precio).HasPrecision(10, 2);

        _ = builder.HasOne<Venta>()
            .WithMany(v => v.Servicios)
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasIndex(x => x.VentaId)
            .HasDatabaseName("IX_Ventas_Servicios_Transaccion");
    }
}

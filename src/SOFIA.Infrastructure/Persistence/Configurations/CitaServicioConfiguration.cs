using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class CitaServicioConfiguration : IEntityTypeConfiguration<CitaServicio>
{
    public void Configure(EntityTypeBuilder<CitaServicio> builder)
    {
        _ = builder.ToTable("Servicios_Citas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Cita_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.PacienteId).HasColumnName("Cliente_ID");
        _ = builder.Property(x => x.ServicioId).HasColumnName("Servicio_ID");
        _ = builder.Property(x => x.SucursalId).HasColumnName("Sucursal_ID");
        _ = builder.Property(x => x.LoteId).HasColumnName("Lote_ID");
        _ = builder.Property(x => x.VentaPreviaId).HasColumnName("VentaPrevia_ID");
    }
}

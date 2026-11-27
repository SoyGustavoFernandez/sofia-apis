using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class AvisoEnviadoConfiguration : IEntityTypeConfiguration<AvisoEnviado>
{
    public void Configure(EntityTypeBuilder<AvisoEnviado> builder)
    {
        _ = builder.ToTable("Plataforma_Avisos_Enviados");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Aviso_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.EmpresaId)
            .HasColumnName("Empresa_ID");

        _ = builder.HasIndex(x => new { x.EmpresaId, x.Fecha, x.Dias })
            .IsUnique()
            .HasDatabaseName("UX_Plataforma_Avisos_Enviados")
            .HasFilter(null);
    }
}

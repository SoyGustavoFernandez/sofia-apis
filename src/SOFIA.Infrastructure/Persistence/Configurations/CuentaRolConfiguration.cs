using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class CuentaRolConfiguration : IEntityTypeConfiguration<CuentaRol>
{
    public void Configure(EntityTypeBuilder<CuentaRol> builder)
    {
        _ = builder.ToTable("Seguridad_Cuentas_Roles");

        _ = builder.HasKey(x => new { x.CuentaId, x.RolId });

        _ = builder.Property(x => x.CuentaId)
            .HasColumnName("Cuenta_ID");

        _ = builder.Property(x => x.RolId)
            .HasColumnName("Rol_ID");

        _ = builder.Property(x => x.AssignedAt)
            .HasColumnName("AssignedAt")
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        _ = builder.Property(x => x.AssignedBy)
            .HasColumnName("AssignedBy")
            .HasMaxLength(100);
    }
}

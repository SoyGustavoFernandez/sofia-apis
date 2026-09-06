using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class CuentaConfiguration : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> builder)
    {
        _ = builder.ToTable("Seguridad_Cuentas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Cuenta_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.EmpleadoId)
            .HasColumnName("Empleado_ID")
            .IsRequired();

        _ = builder.Property(x => x.NombreUsuario)
            .HasColumnName("Nombre_Usuario")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.PasswordHash)
            .HasColumnName("Password_Hash")
            .IsRequired();

        _ = builder.Property(x => x.SecurityStamp)
            .HasColumnName("Security_Stamp")
            .IsRequired();

        _ = builder.Property(x => x.RequiereCambioClave)
            .HasColumnName("Requiere_Cambio_Clave")
            .HasDefaultValue(true);

        _ = builder.Property(x => x.IntentosFallidos)
            .HasColumnName("Intentos_Fallidos")
            .HasDefaultValue(0);

        _ = builder.Property(x => x.BloqueadoHasta)
            .HasColumnName("Bloqueado_Hasta");

        _ = builder.Property(x => x.CuentaActiva)
            .HasColumnName("Cuenta_Activa")
            .HasDefaultValue(true);

        _ = builder.Property(x => x.RecoveryToken)
            .HasColumnName("Recovery_Token")
            .HasMaxLength(100);

        _ = builder.Property(x => x.RecoveryTokenExpiry)
            .HasColumnName("Recovery_Token_Expiry");

        _ = builder.HasOne(x => x.Empleado)
            .WithOne()
            .HasForeignKey<Cuenta>(x => x.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasIndex(x => x.NombreUsuario)
            .IsUnique()
            .HasDatabaseName("IX_Cuentas_Login")
            .HasFilter("IsDeleted = 0 AND Cuenta_Activa = 1");

        _ = builder.HasQueryFilter(x => !x.IsDeleted);

        // Many-to-Many via CuentaRol
        _ = builder.HasMany(x => x.Roles)
            .WithMany(r => r.Cuentas)
            .UsingEntity<CuentaRol>(
                l => l.HasOne(cr => cr.Rol).WithMany().HasForeignKey(cr => cr.RolId),
                r => r.HasOne(cr => cr.Cuenta).WithMany(c => c.CuentasRoles).HasForeignKey(cr => cr.CuentaId)
            );

        // Many-to-Many via CuentaSucursal
        _ = builder.HasMany(x => x.Sucursales)
            .WithMany()
            .UsingEntity<CuentaSucursal>(
                "Seguridad_CuentasSucursales",
                l => l.HasOne(cs => cs.Sucursal).WithMany().HasForeignKey(cs => cs.SucursalId),
                r => r.HasOne(cs => cs.Cuenta).WithMany(c => c.CuentasSucursales).HasForeignKey(cs => cs.CuentaId),
                j =>
                {
                    _ = j.HasKey(cs => new { cs.CuentaId, cs.SucursalId });
                    _ = j.Property(cs => cs.AssignedAt).HasColumnName("Assigned_At");
                    _ = j.Property(cs => cs.AssignedBy).HasColumnName("Assigned_By").HasMaxLength(100);
                }
            );
    }
}

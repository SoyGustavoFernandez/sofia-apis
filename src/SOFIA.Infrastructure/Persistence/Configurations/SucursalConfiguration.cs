using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        _ = builder.ToTable("Sucursales");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Sucursal_ID")
            .ValueGeneratedOnAdd(); // DB generates NEWID()

        _ = builder.Property(x => x.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        _ = builder.Property(x => x.Direccion_Fisica)
            .HasColumnName("Direccion_Fisica")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.Numero_Licencia)
            .HasColumnName("Numero_Licencia")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.EmpresaId)
            .HasColumnName("Empresa_ID");

        _ = builder.HasOne(x => x.Gerente)
            .WithOne(x => x.Sucursal_Gerenciada)
            .HasForeignKey<Sucursal>(x => x.Gerente_ID)
            .IsRequired(false);

        _ = builder.HasIndex(x => new { x.EmpresaId, x.Numero_Licencia }, "UX_Sucursales_Empresa_Licencia")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Empresa_ID] IS NOT NULL");
    }
}

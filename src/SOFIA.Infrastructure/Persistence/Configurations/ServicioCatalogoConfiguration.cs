using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class ServicioCatalogoConfiguration : IEntityTypeConfiguration<ServicioCatalogo>
{
    public void Configure(EntityTypeBuilder<ServicioCatalogo> builder)
    {
        _ = builder.ToTable("Servicios_Catalogo");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Servicio_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.Nombre)
            .HasMaxLength(ServicioCatalogo.NombreMaxLength)
            .IsRequired();

        _ = builder.Property(x => x.Precio)
            .HasPrecision(10, 2);
    }
}

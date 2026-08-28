using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class DigemidCatalogoProductoConfiguration : IEntityTypeConfiguration<DigemidCatalogoProducto>
{
    public void Configure(EntityTypeBuilder<DigemidCatalogoProducto> builder)
    {
        _ = builder.ToTable("DIGEMID_Catalogo_Productos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Catalogo_ID");

        _ = builder.Property(x => x.CodProd)
            .HasColumnName("Cod_Prod")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.Property(x => x.NomProd)
            .HasColumnName("Nom_Prod")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.Concent)
            .HasColumnName("Concent")
            .HasMaxLength(255);

        _ = builder.Property(x => x.FormaFarmaceutica)
            .HasColumnName("Forma_Farmaceutica")
            .HasMaxLength(150);

        _ = builder.Property(x => x.Fraccion)
            .HasColumnName("Fraccion")
            .HasMaxLength(100);

        _ = builder.Property(x => x.RegistroSanitario)
            .HasColumnName("Registro_Sanitario")
            .HasMaxLength(50);

        _ = builder.Property(x => x.Titular)
            .HasColumnName("Titular")
            .HasMaxLength(255);

        _ = builder.Property(x => x.Estado)
            .HasColumnName("Estado")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.HasIndex(x => x.CodProd).IsUnique();
        _ = builder.HasIndex(x => x.NomProd); // Index for searching
    }
}

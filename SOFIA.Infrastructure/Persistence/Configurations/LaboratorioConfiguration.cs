using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class LaboratorioConfiguration : IEntityTypeConfiguration<Laboratorio>
{
    public void Configure(EntityTypeBuilder<Laboratorio> builder)
    {
        _ = builder.ToTable("Laboratorios");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Laboratorio_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.NombreCompania)
            .HasColumnName("Nombre_Compania")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.CodigoIdentificador)
            .HasColumnName("Codigo_Identificador")
            .HasMaxLength(50);

        _ = builder.HasIndex(x => x.NombreCompania).IsUnique();
        _ = builder.HasIndex(x => x.CodigoIdentificador).IsUnique().HasFilter("[Codigo_Identificador] IS NOT NULL");

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

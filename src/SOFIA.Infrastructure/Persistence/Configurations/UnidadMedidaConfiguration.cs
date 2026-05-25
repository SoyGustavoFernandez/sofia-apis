using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        _ = builder.ToTable("Unidades_Medida");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("UoM_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.Codigo)
            .HasColumnName("Codigo_UoM")
            .HasMaxLength(10)
            .IsRequired();

        _ = builder.Property(x => x.Descripcion)
            .HasMaxLength(50)
            .IsRequired();

        // Audit properties are handled by ApplicationDbContext but we can map names if needed
        // The script has them as CreatedAt, CreatedBy, etc. which matches BaseEntity names.

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

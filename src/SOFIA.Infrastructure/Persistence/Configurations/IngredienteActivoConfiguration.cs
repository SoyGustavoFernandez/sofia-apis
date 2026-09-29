using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class IngredienteActivoConfiguration : IEntityTypeConfiguration<IngredienteActivo>
{
    public void Configure(EntityTypeBuilder<IngredienteActivo> builder)
    {
        _ = builder.ToTable("Ingredientes_Activos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Ingrediente_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.DenominacionDci)
            .HasColumnName("Denominacion_DCI")
            .HasMaxLength(255)
            .IsRequired();

        _ = builder.Property(x => x.CodigoAtc)
            .HasColumnName("Codigo_ATC")
            .HasMaxLength(15)
            .IsRequired();

        _ = builder.HasIndex(x => new { x.TenantId, x.DenominacionDci }, "UX_Ingredientes_Activos_Tenant_Denominacion_DCI").IsUnique().HasFilter("[IsDeleted] = 0");
        _ = builder.HasIndex(x => x.CodigoAtc);
    }
}

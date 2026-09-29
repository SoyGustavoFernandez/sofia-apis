using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class JerarquiaUoMConfiguration : IEntityTypeConfiguration<JerarquiaUoM>
{
    public void Configure(EntityTypeBuilder<JerarquiaUoM> builder)
    {
        _ = builder.ToTable("Jerarquia_UoM");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Conversion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.UnidadMayorId)
            .HasColumnName("UoM_Mayor_ID")
            .IsRequired();

        _ = builder.Property(x => x.UnidadMenorId)
            .HasColumnName("UoM_Menor_ID")
            .IsRequired();

        _ = builder.Property(x => x.Multiplicador)
            .HasColumnName("Multiplicador")
            .HasColumnType("DECIMAL(12,4)")
            .IsRequired();

        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.UnidadMayor)
            .WithMany()
            .HasForeignKey(x => x.UnidadMayorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.UnidadMenor)
            .WithMany()
            .HasForeignKey(x => x.UnidadMenorId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasIndex(x => new { x.ProductoId, x.UnidadMayorId, x.UnidadMenorId }, "UX_Jerarquia_UoM_Producto_Mayor_Menor")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        _ = builder.ToTable(t => t.HasCheckConstraint("CHK_Jerarquia_UoM_Multiplicador", "[Multiplicador] > 0"));
    }
}

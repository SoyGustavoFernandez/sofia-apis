using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class MedicamentoConfiguration : IEntityTypeConfiguration<Medicamento>
{
    public void Configure(EntityTypeBuilder<Medicamento> builder)
    {
        _ = builder.ToTable("Medicamentos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Producto_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.CodigoNacional)
            .HasColumnName("Codigo_Nacional")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.NombreComercial)
            .HasColumnName("Nombre_Comercial")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.LaboratorioId)
            .HasColumnName("Laboratorio_ID")
            .IsRequired();

        _ = builder.Property(x => x.UnidadBaseId)
            .HasColumnName("Unidad_Base_ID")
            .IsRequired();

        _ = builder.Property(x => x.CondicionVenta)
            .HasColumnName("Condicion_Venta")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.HasOne(x => x.Laboratorio)
            .WithMany()
            .HasForeignKey(x => x.LaboratorioId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.UnidadBase)
            .WithMany()
            .HasForeignKey(x => x.UnidadBaseId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasIndex(x => x.CodigoNacional).IsUnique();

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

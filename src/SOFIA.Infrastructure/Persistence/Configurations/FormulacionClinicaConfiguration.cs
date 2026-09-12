using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class FormulacionClinicaConfiguration : IEntityTypeConfiguration<FormulacionClinica>
{
    public void Configure(EntityTypeBuilder<FormulacionClinica> builder)
    {
        _ = builder.ToTable("Formulacion_Clinica");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Formulacion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.IngredienteId)
            .HasColumnName("Ingrediente_ID")
            .IsRequired();

        _ = builder.Property(x => x.ConcentracionDosis)
            .HasColumnName("Concentracion_Dosis")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.UnidadMedidaId)
            .HasColumnName("Unidad_Medida_ID")
            .IsRequired();

        _ = builder.Property(x => x.CodigoTeOrange)
            .HasColumnName("Codigo_TE_Orange")
            .HasMaxLength(5);

        // Relationships
        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Ingrediente)
            .WithMany()
            .HasForeignKey(x => x.IngredienteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.UnidadMedida)
            .WithMany()
            .HasForeignKey(x => x.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit & Soft Delete
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

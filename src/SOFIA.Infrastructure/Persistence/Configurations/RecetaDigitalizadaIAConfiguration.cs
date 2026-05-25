using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class RecetaDigitalizadaIAConfiguration : IEntityTypeConfiguration<RecetaDigitalizadaIA>
{
    public void Configure(EntityTypeBuilder<RecetaDigitalizadaIA> builder)
    {
        _ = builder.ToTable("Recetas_Digitalizadas_IA");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Procesamiento_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.RecetaId)
            .HasColumnName("Receta_ID")
            .IsRequired();

        _ = builder.Property(x => x.RutaImagenBlob)
            .HasColumnName("Ruta_Imagen_Blob")
            .HasMaxLength(500)
            .IsRequired();

        _ = builder.Property(x => x.TextoCrudoOcr)
            .HasColumnName("Texto_Crudo_OCR");

        _ = builder.Property(x => x.EntidadesClinicasExtraidas)
            .HasColumnName("Entidades_Clinicas_Extraidas");

        _ = builder.Property(x => x.NivelConfianzaIa)
            .HasColumnName("Nivel_Confianza_IA")
            .HasPrecision(5, 2)
            .IsRequired();

        _ = builder.Property(x => x.RequiereRevisionHumana)
            .HasColumnName("Requiere_Revision_Humana")
            .IsRequired();

        _ = builder.Property(x => x.FechaProcesamiento)
            .HasColumnName("Fecha_Procesamiento")
            .IsRequired();

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasMany(x => x.RegistrosPrivacidad)
            .WithOne(r => r.Procesamiento)
            .HasForeignKey(r => r.ProcesamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

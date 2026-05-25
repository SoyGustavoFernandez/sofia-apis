using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class RegistroPrivacidadPresidioConfiguration : IEntityTypeConfiguration<RegistroPrivacidadPresidio>
{
    public void Configure(EntityTypeBuilder<RegistroPrivacidadPresidio> builder)
    {
        _ = builder.ToTable("Registro_Privacidad_Presidio");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Anonimizacion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ProcesamientoId)
            .HasColumnName("Procesamiento_ID")
            .IsRequired();

        _ = builder.Property(x => x.EntidadDetectada)
            .HasColumnName("Entidad_Detectada")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.TextoOriginalEncriptado)
            .HasColumnName("Texto_Original_Encriptado")
            .IsRequired();

        _ = builder.Property(x => x.TextoReemplazo)
            .HasColumnName("Texto_Reemplazo")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.NivelRiesgoPii)
            .HasColumnName("Nivel_Riesgo_PII")
            .HasPrecision(5, 2);

        _ = builder.Property(x => x.FechaAuditoria)
            .HasColumnName("Fecha_Auditoria")
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
        _ = builder.HasOne(x => x.Procesamiento)
            .WithMany(p => p.RegistrosPrivacidad)
            .HasForeignKey(x => x.ProcesamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

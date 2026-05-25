using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class DIGEMIDActaDestruccionConfiguration : IEntityTypeConfiguration<DIGEMIDActaDestruccion>
{
    public void Configure(EntityTypeBuilder<DIGEMIDActaDestruccion> builder)
    {
        _ = builder.ToTable("DIGEMID_Actas_Destruccion");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Acta_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.NumeroResolucionInterna)
            .HasColumnName("Numero_Resolucion_Interna")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.EmpresaResiduosBiocontaminados)
            .HasColumnName("Empresa_Residuos_Biocontaminados")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.ManifiestoTransporteDoc)
            .HasColumnName("Manifiesto_Transporte_Doc")
            .HasMaxLength(50);

        _ = builder.Property(x => x.FechaEjecucion)
            .HasColumnName("Fecha_Ejecucion")
            .IsRequired();

        _ = builder.Property(x => x.RegenteResponsableId)
            .HasColumnName("Regente_Responsable_ID")
            .IsRequired();

        _ = builder.Property(x => x.RutaActaFirmadaPdf)
            .HasColumnName("Ruta_Acta_Firmada_PDF")
            .HasMaxLength(500);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.RegenteResponsable)
            .WithMany()
            .HasForeignKey(x => x.RegenteResponsableId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasMany(x => x.Detalles)
            .WithOne(d => d.Acta)
            .HasForeignKey(d => d.ActaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

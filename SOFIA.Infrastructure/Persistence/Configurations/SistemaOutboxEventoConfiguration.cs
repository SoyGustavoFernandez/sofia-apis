using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class SistemaOutboxEventoConfiguration : IEntityTypeConfiguration<SistemaOutboxEvento>
{
    public void Configure(EntityTypeBuilder<SistemaOutboxEvento> builder)
    {
        _ = builder.ToTable("Sistema_Outbox_Eventos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Evento_Outbox_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.TipoEvento)
            .HasColumnName("Tipo_Evento")
            .HasMaxLength(100)
            .IsRequired();

        _ = builder.Property(x => x.PayloadJson)
            .HasColumnName("Payload_JSON")
            .IsRequired();

        _ = builder.Property(x => x.FechaCreacion)
            .HasColumnName("Fecha_Creacion")
            .IsRequired();

        _ = builder.Property(x => x.Procesado)
            .HasColumnName("Procesado")
            .IsRequired();

        _ = builder.Property(x => x.FechaProcesamiento)
            .HasColumnName("Fecha_Procesamiento");

        _ = builder.Property(x => x.ErrorPublicacion)
            .HasColumnName("Error_Publicacion");

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

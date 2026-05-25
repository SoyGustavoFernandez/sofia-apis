using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class RecetaMedicaConfiguration : IEntityTypeConfiguration<RecetaMedica>
{
    public void Configure(EntityTypeBuilder<RecetaMedica> builder)
    {
        _ = builder.ToTable("Recetas_Medicas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Receta_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ClienteId)
            .HasColumnName("Cliente_ID")
            .IsRequired();

        _ = builder.Property(x => x.MedicoId)
            .HasColumnName("Medico_ID")
            .IsRequired();

        _ = builder.Property(x => x.FechaExpedicion)
            .HasColumnName("Fecha_Expedicion")
            .IsRequired();

        _ = builder.Property(x => x.RepeticionesMax)
            .HasColumnName("Repeticiones_Max")
            .IsRequired();

        _ = builder.Property(x => x.IndicacionesUso)
            .HasColumnName("Indicaciones_Uso")
            .HasMaxLength(1000);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Medico)
            .WithMany()
            .HasForeignKey(x => x.MedicoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        _ = builder.HasIndex(x => x.ClienteId);
        _ = builder.HasIndex(x => x.MedicoId);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

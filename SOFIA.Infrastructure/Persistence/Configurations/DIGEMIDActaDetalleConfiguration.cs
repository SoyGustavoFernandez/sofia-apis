using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class DIGEMIDActaDetalleConfiguration : IEntityTypeConfiguration<DIGEMIDActaDetalle>
{
    public void Configure(EntityTypeBuilder<DIGEMIDActaDetalle> builder)
    {
        _ = builder.ToTable("DIGEMID_Actas_Detalle");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Detalle_Acta_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.ActaId)
            .HasColumnName("Acta_ID")
            .IsRequired();

        _ = builder.Property(x => x.RegistroCuarentenaId)
            .HasColumnName("Registro_Cuarentena_ID")
            .IsRequired();

        _ = builder.Property(x => x.CantidadDestruida)
            .HasColumnName("Cantidad_Destruida")
            .HasPrecision(12, 4)
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
        _ = builder.HasOne(x => x.Acta)
            .WithMany(a => a.Detalles)
            .HasForeignKey(x => x.ActaId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.RegistroCuarentena)
            .WithMany()
            .HasForeignKey(x => x.RegistroCuarentenaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

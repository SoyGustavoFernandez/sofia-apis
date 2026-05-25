using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class PacienteClienteConfiguration : IEntityTypeConfiguration<PacienteCliente>
{
    public void Configure(EntityTypeBuilder<PacienteCliente> builder)
    {
        _ = builder.ToTable("Pacientes_Clientes");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Cliente_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.DocIdentidadGub)
            .HasColumnName("Doc_Identidad_Gub")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.NombreApellidos)
            .HasColumnName("Nombre_Apellidos")
            .HasMaxLength(200)
            .IsRequired();

        _ = builder.Property(x => x.FechaNacimiento)
            .HasColumnName("Fecha_Nacimiento")
            .IsRequired();

        _ = builder.Property(x => x.ContactoPrimario)
            .HasColumnName("Contacto_Primario")
            .HasMaxLength(100);

        // Audit properties mapping
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Indexes
        _ = builder.HasIndex(x => x.DocIdentidadGub)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

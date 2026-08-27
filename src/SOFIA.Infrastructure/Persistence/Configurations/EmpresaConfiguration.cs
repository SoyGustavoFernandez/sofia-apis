using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        _ = builder.ToTable("Empresas");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Empresa_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        _ = builder.Property(x => x.RUC)
            .HasMaxLength(11)
            .IsFixedLength()
            .IsRequired(false);

        _ = builder.Property(x => x.Estado)
            .HasConversion<int>()
            .IsRequired();

        _ = builder.Property(x => x.FechaInicioTrial)
            .IsRequired();

        _ = builder.Property(x => x.FechaVencimiento)
            .IsRequired();

        _ = builder.Ignore(x => x.EstaVigente);

        // Filtered unique index: two empresas can't share a RUC, but null is allowed (RUC not yet set)
        _ = builder.HasIndex(x => x.RUC)
            .IsUnique()
            .HasFilter("[RUC] IS NOT NULL");

        _ = builder.HasMany(x => x.Sucursales)
            .WithOne(x => x.Empresa)
            .HasForeignKey(x => x.EmpresaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class ServicioClinicoInmunizacionConfiguration : IEntityTypeConfiguration<ServicioClinicoInmunizacion>
{
    public void Configure(EntityTypeBuilder<ServicioClinicoInmunizacion> builder)
    {
        _ = builder.ToTable("Servicios_Clinicos_Inmunizacion");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Evento_Clinico_ID");

        _ = builder.Property(x => x.VentaId)
            .HasColumnName("Transaccion_ID");

        _ = builder.Property(x => x.ClienteId)
            .HasColumnName("Cliente_ID")
            .IsRequired();

        _ = builder.Property(x => x.ProfesionalAdmnId)
            .HasColumnName("Profesional_Admn_ID")
            .IsRequired();

        _ = builder.Property(x => x.ProductoId)
            .HasColumnName("Producto_ID")
            .IsRequired();

        _ = builder.Property(x => x.LoteId)
            .HasColumnName("Lote_ID")
            .IsRequired();

        _ = builder.Property(x => x.ViaAdministracion)
            .HasColumnName("Via_Administracion")
            .HasMaxLength(50)
            .IsRequired();

        _ = builder.Property(x => x.SitioAnatomico)
            .HasColumnName("Sitio_Anatomico")
            .HasMaxLength(100)
            .IsRequired();

        _ = builder.Property(x => x.VolumenDosis)
            .HasColumnName("Volumen_Dosis")
            .HasPrecision(12, 4)
            .IsRequired();

        _ = builder.Property(x => x.FechaAdmnFisica)
            .HasColumnName("Fecha_Admn_Fisica")
            .IsRequired();

        _ = builder.Property(x => x.FechaEntregaVis)
            .HasColumnName("Fecha_Entrega_VIS");

        _ = builder.Property(x => x.ModalidadRegistro)
            .HasConversion(new EnumDescriptionConverter<ModalidadRegistro>())
            .HasColumnName("Modalidad_Registro")
            .HasMaxLength(20)
            .IsRequired();

        // Audit & Soft Delete
        _ = builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        _ = builder.Property(x => x.LastModifiedAt).HasColumnName("LastModifiedAt");
        _ = builder.Property(x => x.LastModifiedBy).HasColumnName("LastModifiedBy");
        _ = builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
        _ = builder.Property(x => x.DeletedAt).HasColumnName("DeletedAt");
        _ = builder.Property(x => x.DeletedBy).HasColumnName("DeletedBy");

        // Relationships
        _ = builder.HasOne(x => x.Venta)
            .WithMany()
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.ProfesionalAdmn)
            .WithMany()
            .HasForeignKey(x => x.ProfesionalAdmnId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Lote)
            .WithMany()
            .HasForeignKey(x => x.LoteId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

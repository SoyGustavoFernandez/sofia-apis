using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public sealed class SunatComprobanteEmitidoConfiguration : IEntityTypeConfiguration<SunatComprobanteEmitido>
{
    public void Configure(EntityTypeBuilder<SunatComprobanteEmitido> builder)
    {
        _ = builder.ToTable("SUNAT_Comprobantes_Emitidos");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Comprobante_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.TransaccionId)
            .HasColumnName("Transaccion_ID")
            .IsRequired();

        _ = builder.Property(x => x.SerieId)
            .HasColumnName("Serie_ID")
            .IsRequired();

        _ = builder.Property(x => x.NumeroCorrelativo)
            .HasColumnName("Numero_Correlativo")
            .IsRequired();

        _ = builder.Property(x => x.FechaEmision)
            .HasColumnName("Fecha_Emision")
            .IsRequired();

        _ = builder.Property(x => x.TipoDocIdentidadCliente)
            .HasColumnName("Tipo_Doc_Identidad_Cliente")
            .HasMaxLength(1)
            .IsRequired();

        _ = builder.Property(x => x.NumeroIdentidadCliente)
            .HasColumnName("Numero_Identidad_Cliente")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.Property(x => x.RazonSocialCliente)
            .HasColumnName("Razon_Social_Cliente")
            .HasMaxLength(200)
            .IsRequired();

        _ = builder.Property(x => x.MontoGravadoIgv)
            .HasColumnName("Monto_Gravado_IGV")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.MontoExonerado)
            .HasColumnName("Monto_Exonerado")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.MontoTotalIgv)
            .HasColumnName("Monto_Total_IGV")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.MontoTotalVenta)
            .HasColumnName("Monto_Total_Venta")
            .HasPrecision(12, 2)
            .IsRequired();

        _ = builder.Property(x => x.HashFirmaDigital)
            .HasColumnName("Hash_Firma_Digital")
            .HasMaxLength(255);

        _ = builder.Property(x => x.EstadoAceptacion)
            .HasColumnName("Estado_Aceptacion")
            .HasMaxLength(20)
            .IsRequired();

        _ = builder.Property(x => x.RutaArchivoXml)
            .HasColumnName("Ruta_Archivo_XML")
            .HasMaxLength(500);

        _ = builder.Property(x => x.RutaArchivoCdr)
            .HasColumnName("Ruta_Archivo_CDR")
            .HasMaxLength(500);

        _ = builder.Property(x => x.UrlPublicaVerificacion)
            .HasColumnName("URL_Publica_Verificacion")
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
        _ = builder.HasOne(x => x.Transaccion)
            .WithMany()
            .HasForeignKey(x => x.TransaccionId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Serie)
            .WithMany()
            .HasForeignKey(x => x.SerieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        _ = builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

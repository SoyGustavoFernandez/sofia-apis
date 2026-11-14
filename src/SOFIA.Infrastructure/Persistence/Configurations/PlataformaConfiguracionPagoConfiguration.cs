using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SOFIA.Domain.Entities;

namespace SOFIA.Infrastructure.Persistence.Configurations;

public class PlataformaConfiguracionPagoConfiguration : IEntityTypeConfiguration<PlataformaConfiguracionPago>
{
    public void Configure(EntityTypeBuilder<PlataformaConfiguracionPago> builder)
    {
        _ = builder.ToTable("Plataforma_Configuracion_Pago");

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("Configuracion_ID")
            .ValueGeneratedOnAdd();

        _ = builder.Property(x => x.WhatsappNumero)
            .HasColumnName("Whatsapp_Numero")
            .HasMaxLength(15)
            .IsUnicode(false)
            .IsRequired();

        _ = builder.Property(x => x.YapeNumero)
            .HasColumnName("Yape_Numero")
            .HasMaxLength(9)
            .IsUnicode(false);

        _ = builder.Property(x => x.Instrucciones)
            .HasMaxLength(PlataformaConfiguracionPago.MaxInstruccionesLength);

        _ = builder.Property(x => x.QrImagen)
            .HasColumnName("Qr_Imagen");

        _ = builder.Property(x => x.QrContentType)
            .HasColumnName("Qr_Content_Type")
            .HasMaxLength(20)
            .IsUnicode(false);
    }
}

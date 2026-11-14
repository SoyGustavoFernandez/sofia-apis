namespace SOFIA.Application.Plataforma;

/// <summary>Payment details any company may read; every field is null until the platform operator saves them.</summary>
public record ConfiguracionPagoDto(string? WhatsappNumero, string? YapeNumero, string? Instrucciones, string? QrDataUrl);

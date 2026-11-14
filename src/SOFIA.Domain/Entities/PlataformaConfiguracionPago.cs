using System.Text.RegularExpressions;
using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

/// <summary>Single row with the platform owner's payment details shown to companies that must pay their subscription.</summary>
public sealed partial class PlataformaConfiguracionPago : BaseEntity
{
    public const int MaxInstruccionesLength = 500;
    public const int MaxQrBytes = 1024 * 1024;
    public const string PngContentType = "image/png";
    public const string JpegContentType = "image/jpeg";

    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF];

    private PlataformaConfiguracionPago() { }

    public string WhatsappNumero { get; private set; } = string.Empty;
    public string? YapeNumero { get; private set; }
    public string? Instrucciones { get; private set; }
    public byte[]? QrImagen { get; private set; }
    public string? QrContentType { get; private set; }

    public static Result<PlataformaConfiguracionPago> Create(string whatsappNumero, string? instrucciones, string? yapeNumero = null)
    {
        var configuracion = new PlataformaConfiguracionPago();
        var result = configuracion.Update(whatsappNumero, instrucciones, yapeNumero);
        return result.IsFailure ? Result.Failure<PlataformaConfiguracionPago>(result.Error) : Result.Success(configuracion);
    }

    public Result Update(string whatsappNumero, string? instrucciones, string? yapeNumero = null)
    {
        // Stored as wa.me expects it: country code plus number, digits only
        if (string.IsNullOrWhiteSpace(whatsappNumero) || !WhatsappRegex().IsMatch(whatsappNumero))
        {
            return Result.Failure(Error.Validation("ConfiguracionPago.WhatsappNumero", "WhatsApp number must be 8 to 15 digits including the country code."));
        }

        // Fallback for payers whose camera cannot read the QR: the wallet number typed by hand
        var yape = string.IsNullOrWhiteSpace(yapeNumero) ? null : yapeNumero;
        if (yape is not null && !YapeRegex().IsMatch(yape))
        {
            return Result.Failure(Error.Validation("ConfiguracionPago.YapeNumero", "Yape number must be exactly 9 digits."));
        }

        var texto = string.IsNullOrWhiteSpace(instrucciones) ? null : instrucciones.Trim();
        if (texto?.Length > MaxInstruccionesLength)
        {
            return Result.Failure(Error.Validation("ConfiguracionPago.Instrucciones", $"Instructions cannot exceed {MaxInstruccionesLength} characters."));
        }

        WhatsappNumero = whatsappNumero;
        YapeNumero = yape;
        Instrucciones = texto;
        return Result.Success();
    }

    // The content type comes from the file signature, never from what the client declares
    public Result CambiarQr(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0 || imagen.Length > MaxQrBytes)
        {
            return Result.Failure(Error.Validation("ConfiguracionPago.Qr.Tamano", "The QR image must be between 1 byte and 1 MB."));
        }

        var contentType = DetectarContentType(imagen);
        if (contentType is null)
        {
            return Result.Failure(Error.Validation("ConfiguracionPago.Qr.Formato", "The QR image must be a PNG or JPEG file."));
        }

        QrImagen = imagen;
        QrContentType = contentType;
        return Result.Success();
    }

    private static string? DetectarContentType(byte[] imagen)
    {
        if (imagen.AsSpan().StartsWith(_pngSignature))
        {
            return PngContentType;
        }

        return imagen.AsSpan().StartsWith(_jpegSignature) ? JpegContentType : null;
    }

    [GeneratedRegex("^[0-9]{8,15}$")]
    private static partial Regex WhatsappRegex();

    [GeneratedRegex("^[0-9]{9}$")]
    private static partial Regex YapeRegex();
}

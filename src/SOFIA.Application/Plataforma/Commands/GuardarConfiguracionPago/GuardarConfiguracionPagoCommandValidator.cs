using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;

public class GuardarConfiguracionPagoCommandValidator : AbstractValidator<GuardarConfiguracionPagoCommand>
{
    public GuardarConfiguracionPagoCommandValidator()
    {
        _ = RuleFor(x => x.WhatsappNumero)
            .NotEmpty()
            .Matches("^[0-9]{8,15}$").WithMessage("WhatsApp number must be 8 to 15 digits including the country code.");

        _ = RuleFor(x => x.YapeNumero)
            .Matches("^[0-9]{9}$").WithMessage("Yape number must be exactly 9 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.YapeNumero));

        _ = RuleFor(x => x.Instrucciones)
            .MaximumLength(PlataformaConfiguracionPago.MaxInstruccionesLength);

        _ = When(x => x.QrImagen is not null, () =>
            RuleFor(x => x.QrImagen!)
                .Must(imagen => imagen.Length is > 0 and <= PlataformaConfiguracionPago.MaxQrBytes)
                .WithMessage("The QR image must be between 1 byte and 1 MB."));
    }
}

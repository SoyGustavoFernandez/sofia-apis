using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;

public class UpdateSerieFiscalCommandValidator : AbstractValidator<UpdateSerieFiscalCommand>
{
    public UpdateSerieFiscalCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.SucursalId)
            .NotEmpty().WithMessage("Branch is required.");

        _ = RuleFor(v => v.TipoComprobante)
            .Must(t => SunatSerieFiscal.TiposConfigurables.Contains(t)).WithMessage("Unsupported document type for a series.");

        _ = RuleFor(v => v.PrefijoSerie)
            .NotEmpty().WithMessage("Prefix is required.")
            .Matches("^[A-Z0-9]{4}$").WithMessage("Prefix must be 4 uppercase letters or digits.")
            .Must((v, prefijo) => SunatSerieFiscal.EsPrefijoValido(v.TipoComprobante, prefijo)).WithMessage("Prefix does not start with the letter required by the document type.");

        _ = RuleFor(v => v.CorrelativoActual)
            .GreaterThanOrEqualTo(0).WithMessage("Current correlative must be greater than or equal to zero.");

        _ = RuleFor(v => v.EstadoSerie)
            .Must(SunatSerieFiscal.EsEstadoValido).WithMessage("Status must be Activa or Inactiva.");
    }
}

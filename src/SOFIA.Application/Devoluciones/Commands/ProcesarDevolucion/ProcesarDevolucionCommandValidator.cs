using FluentValidation;

namespace SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

public class ProcesarDevolucionCommandValidator : AbstractValidator<ProcesarDevolucionCommand>
{
    public ProcesarDevolucionCommandValidator()
    {
        _ = RuleFor(v => v.ComprobanteOrigenId).NotEmpty();
        _ = RuleFor(v => v.EmpleadoAutorizaId).NotEmpty();
        _ = RuleFor(v => v.MotivoSunatCatalogo).NotEmpty().MaximumLength(2);
        _ = RuleFor(v => v.SustentoDescriptivo).NotEmpty().MaximumLength(255);
        _ = RuleFor(v => v.Detalles).NotEmpty();
        _ = RuleForEach(v => v.Detalles).ChildRules(detalles =>
        {
            _ = detalles.RuleFor(d => d.DetalleVentaId).NotEmpty();
            _ = detalles.RuleFor(d => d.CantidadDevuelta).GreaterThan(0);
        });
    }
}

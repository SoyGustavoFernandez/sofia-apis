using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.CatalogoServicios.Commands.UpdateServicioCatalogo;

public class UpdateServicioCatalogoCommandValidator : AbstractValidator<UpdateServicioCatalogoCommand>
{
    public UpdateServicioCatalogoCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.Nombre).NotEmpty().MaximumLength(ServicioCatalogo.NombreMaxLength);
        _ = RuleFor(v => v.Precio).GreaterThan(0).LessThanOrEqualTo(ServicioCatalogo.PrecioMaximo);
    }
}

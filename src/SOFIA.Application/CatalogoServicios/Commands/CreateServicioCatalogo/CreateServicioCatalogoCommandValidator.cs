using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;

public class CreateServicioCatalogoCommandValidator : AbstractValidator<CreateServicioCatalogoCommand>
{
    public CreateServicioCatalogoCommandValidator()
    {
        _ = RuleFor(v => v.Nombre).NotEmpty().MaximumLength(ServicioCatalogo.NombreMaxLength);
        _ = RuleFor(v => v.Precio).GreaterThan(0).LessThanOrEqualTo(ServicioCatalogo.PrecioMaximo);
    }
}

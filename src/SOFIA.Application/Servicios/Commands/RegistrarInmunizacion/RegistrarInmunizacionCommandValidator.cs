using FluentValidation;

namespace SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;

public class RegistrarInmunizacionCommandValidator : AbstractValidator<RegistrarInmunizacionCommand>
{
    public RegistrarInmunizacionCommandValidator() =>
        // The lot expiry is judged at this moment, so it cannot be left out
        _ = RuleFor(x => x.FechaAdmnFisica).NotEmpty();
}

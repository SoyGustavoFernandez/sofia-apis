using FluentValidation;

namespace SOFIA.Application.Citas.Commands.ReprogramarCita;

public class ReprogramarCitaCommandValidator : AbstractValidator<ReprogramarCitaCommand>
{
    public ReprogramarCitaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.ServicioId).NotEmpty();
        _ = RuleFor(v => v.FechaHora).NotEmpty();
    }
}

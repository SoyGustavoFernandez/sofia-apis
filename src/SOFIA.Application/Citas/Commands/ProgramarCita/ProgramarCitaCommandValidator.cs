using FluentValidation;

namespace SOFIA.Application.Citas.Commands.ProgramarCita;

public class ProgramarCitaCommandValidator : AbstractValidator<ProgramarCitaCommand>
{
    public ProgramarCitaCommandValidator()
    {
        _ = RuleFor(v => v.PacienteId).NotEmpty();
        _ = RuleFor(v => v.ServicioId).NotEmpty();
        _ = RuleFor(v => v.FechaHora).NotEmpty();
    }
}

using FluentValidation;

namespace SOFIA.Application.Citas.Commands.CerrarCita;

public class CerrarCitaCommandValidator : AbstractValidator<CerrarCitaCommand>
{
    public CerrarCitaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.Estado).IsInEnum();
    }
}

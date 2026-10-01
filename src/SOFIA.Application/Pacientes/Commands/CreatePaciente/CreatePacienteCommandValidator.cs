using FluentValidation;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Pacientes.Commands.CreatePaciente;

public class CreatePacienteCommandValidator : AbstractValidator<CreatePacienteCommand>
{
    public CreatePacienteCommandValidator()
    {
        _ = RuleFor(v => v.TipoDocumento).NotEmpty().Must(TipoDocumentoIdentidad.EsValido)
            .WithMessage("TipoDocumento must be one of: 1 (DNI), 4 (CE), 6 (RUC), 7 (Pasaporte).");
        _ = RuleFor(v => v.DocIdentidadGub).NotEmpty().MaximumLength(50);
        _ = RuleFor(v => v.DocIdentidadGub)
            .Must((v, doc) => TipoDocumentoIdentidad.EsNumeroValido(v.TipoDocumento, doc))
            .When(v => TipoDocumentoIdentidad.EsValido(v.TipoDocumento))
            .WithMessage("DocIdentidadGub does not match the format of its document type.");
        _ = RuleFor(v => v.NombreApellidos).NotEmpty().MaximumLength(200);
        _ = RuleFor(v => v.ContactoPrimario).MaximumLength(100);
    }
}

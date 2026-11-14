using FluentValidation;

namespace SOFIA.Application.Empresas.Commands.SuspenderEmpresa;

public class SuspenderEmpresaCommandValidator : AbstractValidator<SuspenderEmpresaCommand>
{
    public SuspenderEmpresaCommandValidator() => _ = RuleFor(x => x.Id).NotEmpty();
}

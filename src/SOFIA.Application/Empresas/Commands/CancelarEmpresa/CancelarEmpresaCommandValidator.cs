using FluentValidation;

namespace SOFIA.Application.Empresas.Commands.CancelarEmpresa;

public class CancelarEmpresaCommandValidator : AbstractValidator<CancelarEmpresaCommand>
{
    public CancelarEmpresaCommandValidator() => _ = RuleFor(x => x.Id).NotEmpty();
}

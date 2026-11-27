using FluentValidation;

namespace SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;

public class DeleteServicioCatalogoCommandValidator : AbstractValidator<DeleteServicioCatalogoCommand>
{
    public DeleteServicioCatalogoCommandValidator() => _ = RuleFor(v => v.Id).NotEmpty();
}

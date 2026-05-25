using FluentValidation;

namespace SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;

public class CompletarOrdenMagistralCommandValidator : AbstractValidator<CompletarOrdenMagistralCommand>
{
    public CompletarOrdenMagistralCommandValidator()
    {
        _ = RuleFor(x => x.OrdenId).NotEmpty();
        _ = RuleFor(x => x.NumeroLoteMfr).NotEmpty().MaximumLength(50);
        _ = RuleFor(x => x.FechaCaducidad).GreaterThan(DateTimeOffset.UtcNow);
    }
}

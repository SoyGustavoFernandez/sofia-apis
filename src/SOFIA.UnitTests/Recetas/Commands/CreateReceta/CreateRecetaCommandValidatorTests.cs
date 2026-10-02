using FluentValidation.TestHelper;
using SOFIA.Application.Recetas.Commands.CreateReceta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandValidatorTests
{
    private readonly CreateRecetaCommandValidator _validator = new();

    private static CreateRecetaCommand Command(string? numeroReceta) =>
        new(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 0, null, numeroReceta);

    [Fact]
    public void Should_Have_Error_When_NumeroReceta_Exceeds_MaxLength() =>
        _ = _validator.TestValidate(Command(new string('7', RecetaMedica.NumeroRecetaMaxLength + 1))).ShouldHaveValidationErrorFor(x => x.NumeroReceta);

    [Fact]
    public void Should_Not_Have_Error_When_NumeroReceta_Is_At_MaxLength() =>
        _validator.TestValidate(Command(new string('7', RecetaMedica.NumeroRecetaMaxLength))).ShouldNotHaveValidationErrorFor(x => x.NumeroReceta);

    [Fact]
    public void Should_Not_Have_Error_When_NumeroReceta_Is_Missing() =>
        _validator.TestValidate(Command(null)).ShouldNotHaveAnyValidationErrors();
}

using FluentAssertions;
using SOFIA.Application.FormulacionesClinicas.Commands.Update;

namespace SOFIA.UnitTests.FormulacionesClinicas.Commands.UpdateFormulacionClinica;

public class UpdateFormulacionClinicaCommandValidatorTests
{
    private readonly UpdateFormulacionClinicaCommandValidator _validator = new();

    private static UpdateFormulacionClinicaCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        IngredienteId = Guid.NewGuid(),
        ConcentracionDosis = 100m,
        UnidadMedidaId = Guid.NewGuid(),
        CodigoTeOrange = "TE001",
    };

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid()
    {
        var result = _validator.Validate(ValidCommand());

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFormulacionClinicaCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIngredienteIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { IngredienteId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFormulacionClinicaCommand.IngredienteId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenConcentracionDosisNotPositive()
    {
        var result = _validator.Validate(ValidCommand() with { ConcentracionDosis = 0 });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFormulacionClinicaCommand.ConcentracionDosis));
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadMedidaIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { UnidadMedidaId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFormulacionClinicaCommand.UnidadMedidaId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodigoTeOrangeTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoTeOrange = "TOOLONG" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFormulacionClinicaCommand.CodigoTeOrange));
    }
}

using FluentAssertions;
using SOFIA.Application.FormulacionesClinicas.Commands.Create;

namespace SOFIA.UnitTests.FormulacionesClinicas.Commands.CreateFormulacionClinica;

public class CreateFormulacionClinicaCommandValidatorTests
{
    private readonly CreateFormulacionClinicaCommandValidator _validator = new();

    private static CreateFormulacionClinicaCommand ValidCommand() => new()
    {
        ProductoId = Guid.NewGuid(),
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
    public void Validate_ShouldFail_WhenProductoIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { ProductoId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormulacionClinicaCommand.ProductoId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIngredienteIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { IngredienteId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormulacionClinicaCommand.IngredienteId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenConcentracionDosisNotPositive()
    {
        var result = _validator.Validate(ValidCommand() with { ConcentracionDosis = 0 });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormulacionClinicaCommand.ConcentracionDosis));
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadMedidaIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { UnidadMedidaId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormulacionClinicaCommand.UnidadMedidaId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodigoTeOrangeTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoTeOrange = "TOOLONG" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFormulacionClinicaCommand.CodigoTeOrange));
    }

    [Fact]
    public void Validate_ShouldPass_WhenCodigoTeOrangeIsNull()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoTeOrange = null });

        _ = result.IsValid.Should().BeTrue();
    }
}

using FluentAssertions;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.CreateMedicamento;

public class CreateMedicamentoCommandValidatorTests
{
    private readonly CreateMedicamentoCommandValidator _validator = new();

    private static CreateMedicamentoCommand ValidCommand() => new(
        "COD-001", "Paracetamol 500mg", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaSimple);

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid()
    {
        var result = _validator.Validate(ValidCommand());

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodigoNacionalEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoNacional = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.CodigoNacional));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodigoNacionalTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoNacional = new string('x', Medicamento.CodigoNacionalMaxLength + 1) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.CodigoNacional));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNombreComercialEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { NombreComercial = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.NombreComercial));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNombreComercialTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { NombreComercial = new string('x', Medicamento.NombreComercialMaxLength + 1) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.NombreComercial));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLaboratorioIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { LaboratorioId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.LaboratorioId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadBaseIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { UnidadBaseId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.UnidadBaseId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCondicionVentaNotInEnum()
    {
        var result = _validator.Validate(ValidCommand() with { CondicionVenta = (CondicionVenta)99 });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMedicamentoCommand.CondicionVenta));
    }
}

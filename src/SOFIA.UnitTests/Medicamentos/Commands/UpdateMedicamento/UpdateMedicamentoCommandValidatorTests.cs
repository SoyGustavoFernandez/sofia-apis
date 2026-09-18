using FluentAssertions;
using SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.UpdateMedicamento;

public class UpdateMedicamentoCommandValidatorTests
{
    private readonly UpdateMedicamentoCommandValidator _validator = new();

    private static UpdateMedicamentoCommand ValidCommand() => new(
        Guid.NewGuid(), "COD-001", "Paracetamol 500mg", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaSimple, 15m);

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
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCodigoNacionalEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { CodigoNacional = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.CodigoNacional));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNombreComercialTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { NombreComercial = new string('x', Medicamento.NombreComercialMaxLength + 1) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.NombreComercial));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLaboratorioIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { LaboratorioId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.LaboratorioId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadBaseIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { UnidadBaseId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.UnidadBaseId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCondicionVentaNotInEnum()
    {
        var result = _validator.Validate(ValidCommand() with { CondicionVenta = (CondicionVenta)99 });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.CondicionVenta));
    }

    [Fact]
    public void Validate_ShouldFail_WhenPrecioVentaBaseIsNegative()
    {
        var result = _validator.Validate(ValidCommand() with { PrecioVentaBase = -1m });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMedicamentoCommand.PrecioVentaBase));
    }
}

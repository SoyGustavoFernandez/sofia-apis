using FluentAssertions;
using SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Onboarding;

public class RegistrarPrimerProductoCommandValidatorTests
{
    private readonly RegistrarPrimerProductoCommandValidator _validator = new();

    private static RegistrarPrimerProductoCommand ValidCommand() => new(
        "7750000000001", "Paracetamol 500 mg", "Laboratorio Andino", Guid.NewGuid(), CondicionVenta.VentaLibreOTC, 1.5m, "L-001", DateTimeOffset.UtcNow.AddYears(1), 100m);

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsComplete() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenARequiredTextIsBlank(string blank)
    {
        var command = ValidCommand() with { CodigoNacional = blank, NombreComercial = blank, LaboratorioNombre = blank, NumeroLote = blank };

        var errors = _validator.Validate(command).Errors.Select(e => e.PropertyName);

        _ = errors.Should().Contain(["CodigoNacional", "NombreComercial", "LaboratorioNombre", "NumeroLote"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPriceOrQuantityIsNotPositive(int value)
    {
        var command = ValidCommand() with { PrecioVentaBase = value, Cantidad = value };

        var errors = _validator.Validate(command).Errors.Select(e => e.PropertyName);

        _ = errors.Should().Contain(["PrecioVentaBase", "Cantidad"]);
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpiryIsPastOrUnitIsMissing()
    {
        var command = ValidCommand() with { FechaCaducidad = DateTimeOffset.UtcNow.AddDays(-3), UnidadBaseId = Guid.Empty };

        var errors = _validator.Validate(command).Errors.Select(e => e.PropertyName);

        _ = errors.Should().Contain(["FechaCaducidad", "UnidadBaseId"]);
    }
}

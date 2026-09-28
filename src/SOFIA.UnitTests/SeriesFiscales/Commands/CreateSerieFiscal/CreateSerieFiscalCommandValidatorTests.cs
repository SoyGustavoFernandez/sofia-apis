using FluentAssertions;
using SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Commands.CreateSerieFiscal;

public class CreateSerieFiscalCommandValidatorTests
{
    private readonly CreateSerieFiscalCommandValidator _validator = new();

    private static CreateSerieFiscalCommand ValidCommand() =>
        new(Guid.NewGuid(), TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva);

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenSucursalEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { SucursalId = Guid.Empty });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.SucursalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("b001")]
    [InlineData("B01")]
    [InlineData("B0001")]
    [InlineData("F001")]
    public void Validate_ShouldFail_WhenBoletaPrefixIsInvalid(string prefijo)
    {
        var result = _validator.Validate(ValidCommand() with { PrefijoSerie = prefijo });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.PrefijoSerie));
    }

    [Theory]
    [InlineData(TipoComprobante.Factura, "F001")]
    [InlineData(TipoComprobante.NotaCredito, "BC01")]
    [InlineData(TipoComprobante.NotaCredito, "FC01")]
    [InlineData(TipoComprobante.Proforma, "PR01")]
    public void Validate_ShouldPass_WhenPrefixMatchesType(TipoComprobante tipo, string prefijo) =>
        _ = _validator.Validate(ValidCommand() with { TipoComprobante = tipo, PrefijoSerie = prefijo }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(TipoComprobante.Factura, "B001")]
    [InlineData(TipoComprobante.NotaCredito, "PC01")]
    public void Validate_ShouldFail_WhenPrefixDoesNotMatchType(TipoComprobante tipo, string prefijo)
    {
        var result = _validator.Validate(ValidCommand() with { TipoComprobante = tipo, PrefijoSerie = prefijo });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.PrefijoSerie));
    }

    [Theory]
    [InlineData(TipoComprobante.Ticket)]
    [InlineData(TipoComprobante.NotaDebito)]
    public void Validate_ShouldFail_WhenTypeIsNotConfigurable(TipoComprobante tipo)
    {
        var result = _validator.Validate(ValidCommand() with { TipoComprobante = tipo });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.TipoComprobante));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCorrelativeIsNegative()
    {
        var result = _validator.Validate(ValidCommand() with { CorrelativoActual = -1 });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.CorrelativoActual));
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsUnknown()
    {
        var result = _validator.Validate(ValidCommand() with { EstadoSerie = "Pausada" });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSerieFiscalCommand.EstadoSerie));
    }
}

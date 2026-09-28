using FluentAssertions;
using SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Commands.UpdateSerieFiscal;

public class UpdateSerieFiscalCommandValidatorTests
{
    private readonly UpdateSerieFiscalCommandValidator _validator = new();

    private static UpdateSerieFiscalCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), TipoComprobante.Factura, "F001", 10, SunatSerieFiscal.EstadoInactiva);

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSerieFiscalCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenFacturaPrefixStartsWithB()
    {
        var result = _validator.Validate(ValidCommand() with { PrefijoSerie = "B001" });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSerieFiscalCommand.PrefijoSerie));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCorrelativeIsNegative()
    {
        var result = _validator.Validate(ValidCommand() with { CorrelativoActual = -5 });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSerieFiscalCommand.CorrelativoActual));
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsUnknown()
    {
        var result = _validator.Validate(ValidCommand() with { EstadoSerie = "" });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSerieFiscalCommand.EstadoSerie));
    }
}

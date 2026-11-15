using FluentValidation.TestHelper;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

namespace SOFIA.UnitTests.Devoluciones.Queries;

public class GetDevolucionesQueryValidatorTests
{
    private readonly GetDevolucionesQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WithNoFilters() => _validator.TestValidate(new GetDevolucionesQuery()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_ShouldPass_WithEveryFilter()
    {
        var query = new GetDevolucionesQuery
        {
            EmpleadoAutorizaId = Guid.NewGuid(),
            MotivoSunatCatalogo = "07",
            NumeroNotaCredito = "BC01-00000007",
            MontoMin = 0,
            MontoMax = 100,
        };

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmpleadoIsTheEmptyGuid()
    {
        var result = _validator.TestValidate(new GetDevolucionesQuery { EmpleadoAutorizaId = Guid.Empty });
        _ = result.ShouldHaveValidationErrorFor(x => x.EmpleadoAutorizaId);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("007")]
    public void Validate_ShouldFail_WhenMotivoIsNotTwoCharacters(string motivo)
    {
        var result = _validator.TestValidate(new GetDevolucionesQuery { MotivoSunatCatalogo = motivo });
        _ = result.ShouldHaveValidationErrorFor(x => x.MotivoSunatCatalogo);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumeroNotaCreditoIsTooLong()
    {
        var result = _validator.TestValidate(new GetDevolucionesQuery { NumeroNotaCredito = new string('B', 21) });
        _ = result.ShouldHaveValidationErrorFor(x => x.NumeroNotaCredito);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMontoMinIsNegative()
    {
        var result = _validator.TestValidate(new GetDevolucionesQuery { MontoMin = -1 });
        _ = result.ShouldHaveValidationErrorFor(x => x.MontoMin);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMontoMaxIsBelowMontoMin()
    {
        var result = _validator.TestValidate(new GetDevolucionesQuery { MontoMin = 50, MontoMax = 49.99m });
        _ = result.ShouldHaveValidationErrorFor(x => x.MontoMax);
    }

    [Fact]
    public void Validate_ShouldPass_WhenMontoMaxEqualsMontoMin() => _validator.TestValidate(new GetDevolucionesQuery { MontoMin = 50, MontoMax = 50 }).ShouldNotHaveAnyValidationErrors();
}

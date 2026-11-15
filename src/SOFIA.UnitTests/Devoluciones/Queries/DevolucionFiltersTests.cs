using FluentAssertions;
using SOFIA.Application.Devoluciones.Common;

namespace SOFIA.UnitTests.Devoluciones.Queries;

public class DevolucionFiltersTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseNumeroNotaCredito_ShouldReturnNothing_WhenBlank(string? numero) => _ = DevolucionFilters.ParseNumeroNotaCredito(numero).Should().Be((null, null));

    [Theory]
    [InlineData("BC01-00000007", "BC01", 7)]
    [InlineData("  BC01 - 7  ", "BC01", 7)]
    [InlineData("BC01-0007", "BC01", 7)]
    public void ParseNumeroNotaCredito_ShouldSplitAPrintedNumber(string numero, string prefijo, int correlativo) => _ = DevolucionFilters.ParseNumeroNotaCredito(numero).Should().Be((prefijo, correlativo));

    [Theory]
    [InlineData("BC01")]
    [InlineData("bc")]
    public void ParseNumeroNotaCredito_ShouldTreatTextWithoutDigitsOnlyAsASeriesPrefix(string numero) => _ = DevolucionFilters.ParseNumeroNotaCredito(numero).Should().Be((numero, null));

    [Theory]
    [InlineData("7", 7)]
    [InlineData("00000007", 7)]
    public void ParseNumeroNotaCredito_ShouldTreatAPlainNumberAsACorrelative(string numero, int correlativo) => _ = DevolucionFilters.ParseNumeroNotaCredito(numero).Should().Be((null, correlativo));

    [Theory]
    [InlineData("BC01-", "BC01")]
    [InlineData("BC01-abc", "BC01")]
    public void ParseNumeroNotaCredito_ShouldKeepThePrefixAlone_WhenTheCorrelativeIsMissingOrNotANumber(string numero, string prefijo) => _ = DevolucionFilters.ParseNumeroNotaCredito(numero).Should().Be((prefijo, null));

    [Fact]
    public void ParseNumeroNotaCredito_ShouldKeepTheCorrelativeAlone_WhenThePrefixIsMissing() => _ = DevolucionFilters.ParseNumeroNotaCredito("-7").Should().Be((null, 7));
}

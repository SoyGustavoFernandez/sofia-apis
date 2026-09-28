using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Recetas.Domain;

public class RecetaMedicaTests
{
    private static RecetaMedica CrearReceta(int repeticionesMax = 0) =>
        RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), repeticionesMax).Value!;

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public void PuedeDispensar_ShouldSucceed_WhenProductIsOtc(int previas) =>
        _ = CrearReceta().PuedeDispensar(previas, CondicionVenta.VentaLibreOTC).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida)]
    [InlineData(CondicionVenta.Estupefaciente)]
    public void PuedeDispensar_ShouldSucceed_WhenControlledPrescriptionIsUnused(CondicionVenta condicion) =>
        _ = CrearReceta(repeticionesMax: 3).PuedeDispensar(0, condicion).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida)]
    [InlineData(CondicionVenta.Estupefaciente)]
    public void PuedeDispensar_ShouldFail_WhenControlledPrescriptionWasAlreadyDispensed(CondicionVenta condicion)
    {
        // Refills never apply to retained or narcotic prescriptions
        var result = CrearReceta(repeticionesMax: 3).PuedeDispensar(1, condicion);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Agotada");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(2, 2)]
    public void PuedeDispensar_ShouldSucceed_WhenSimplePrescriptionHasRefillsLeft(int repeticionesMax, int previas) =>
        _ = CrearReceta(repeticionesMax).PuedeDispensar(previas, CondicionVenta.RecetaSimple).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    public void PuedeDispensar_ShouldFail_WhenSimplePrescriptionIsExhausted(int repeticionesMax, int previas)
    {
        var result = CrearReceta(repeticionesMax).PuedeDispensar(previas, CondicionVenta.RecetaSimple);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Agotada");
    }
}

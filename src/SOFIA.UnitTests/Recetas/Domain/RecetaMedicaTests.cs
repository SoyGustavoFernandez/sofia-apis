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

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Create_ShouldLeaveNumeroRecetaEmpty_WhenNotProvided() =>
        _ = CrearReceta().NumeroReceta.Should().BeNull();

    [Theory]
    [InlineData("  RE-001234  ", "RE-001234")]
    [InlineData("RE-001234", "RE-001234")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    public void Create_ShouldTrimNumeroReceta_AndStoreBlankAsNull(string numero, string? esperado)
    {
        var result = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), Hoy, 0, null, numero);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.NumeroReceta.Should().Be(esperado);
    }

    [Fact]
    public void Create_ShouldAcceptNumeroReceta_AtTheMaximumLengthAfterTrimming()
    {
        var numero = new string('7', RecetaMedica.NumeroRecetaMaxLength);

        var result = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), Hoy, 0, null, $" {numero} ");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.NumeroReceta.Should().Be(numero);
    }

    [Fact]
    public void Create_ShouldFail_WhenNumeroRecetaExceedsTheMaximumLength()
    {
        var result = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), Hoy, 0, null, new string('7', RecetaMedica.NumeroRecetaMaxLength + 1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("RecetaMedica.NumeroReceta");
    }

    [Theory]
    [InlineData(" RE-9 ", "RE-9")]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    public void Update_ShouldReplaceNumeroReceta_TrimmedOrCleared(string? numero, string? esperado)
    {
        var receta = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), Hoy, 0, null, "RE-1").Value!;

        var result = receta.Update(receta.ClienteId, receta.MedicoId, Hoy, 0, null, numero);

        _ = result.IsSuccess.Should().BeTrue();
        _ = receta.NumeroReceta.Should().Be(esperado);
    }

    [Fact]
    public void Update_ShouldFailAndKeepTheNumber_WhenNumeroRecetaExceedsTheMaximumLength()
    {
        var receta = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), Hoy, 0, null, "RE-1").Value!;

        var result = receta.Update(receta.ClienteId, receta.MedicoId, Hoy, 0, null, new string('7', RecetaMedica.NumeroRecetaMaxLength + 1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("RecetaMedica.NumeroReceta");
        _ = receta.NumeroReceta.Should().Be("RE-1");
    }
}

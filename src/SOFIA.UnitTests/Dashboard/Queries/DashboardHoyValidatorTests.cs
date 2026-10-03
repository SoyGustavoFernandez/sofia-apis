using FluentAssertions;
using SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;
using SOFIA.Application.Dashboard.Queries.GetDashboardInventario;
using SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

namespace SOFIA.UnitTests.Dashboard.Queries;

public class DashboardHoyValidatorTests
{
    private readonly GetDashboardVentasQueryValidator _ventas = new();
    private readonly GetDashboardInventarioQueryValidator _inventario = new();
    private readonly GetDashboardDevolucionesQueryValidator _devoluciones = new();

    // Start of the current local day in the given offset, as the SPA sends it
    private static DateTimeOffset InicioDelDia(double offsetHoras)
    {
        var offset = TimeSpan.FromHours(offsetHoras);
        var ahora = DateTimeOffset.UtcNow.ToOffset(offset);
        return new DateTimeOffset(ahora.Year, ahora.Month, ahora.Day, 0, 0, 0, offset);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(-12)]
    [InlineData(14)]
    public void Validate_ShouldPass_WhenHoyIsTheStartOfTheLocalDayInAnyTimeZone(double offsetHoras)
    {
        var hoy = InicioDelDia(offsetHoras);

        _ = _ventas.Validate(new GetDashboardVentasQuery(hoy)).IsValid.Should().BeTrue();
        _ = _inventario.Validate(new GetDashboardInventarioQuery(hoy)).IsValid.Should().BeTrue();
        _ = _devoluciones.Validate(new GetDashboardDevolucionesQuery(hoy)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(3)]
    [InlineData(-400)]
    public void Validate_ShouldFailOnHoy_WhenItIsDaysAwayFromNow(int dias)
    {
        var hoy = DateTimeOffset.UtcNow.AddDays(dias);

        _ = _ventas.Validate(new GetDashboardVentasQuery(hoy)).Errors.Should().ContainSingle(e => e.PropertyName == "Hoy");
        _ = _inventario.Validate(new GetDashboardInventarioQuery(hoy)).Errors.Should().ContainSingle(e => e.PropertyName == "Hoy");
        _ = _devoluciones.Validate(new GetDashboardDevolucionesQuery(hoy)).Errors.Should().ContainSingle(e => e.PropertyName == "Hoy");
    }

    [Fact]
    public void Validate_ShouldFail_WhenHoyWasNotSent()
    {
        _ = _ventas.Validate(new GetDashboardVentasQuery(default)).IsValid.Should().BeFalse();
        _ = _inventario.Validate(new GetDashboardInventarioQuery(default)).IsValid.Should().BeFalse();
        _ = _devoluciones.Validate(new GetDashboardDevolucionesQuery(default)).IsValid.Should().BeFalse();
    }
}

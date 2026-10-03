using FluentAssertions;
using SOFIA.Application.Dashboard.Common;

namespace SOFIA.UnitTests.Dashboard.Common;

public class DashboardDiasTests
{
    private static readonly DateTimeOffset HoyLima = new(2026, 10, 2, 0, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Ventanas_ShouldReturnOneWindowPerDayOldestFirst_EndingToday()
    {
        var ventanas = DashboardDias.Ventanas(HoyLima, 7);

        _ = ventanas.Should().HaveCount(7);
        _ = ventanas.Select(v => v.Desde).Should().Equal(Enumerable.Range(0, 7).Select(i => HoyLima.AddDays(i - 6)));
        _ = ventanas[^1].Desde.Should().Be(HoyLima);
    }

    [Fact]
    public void Ventanas_ShouldBuildHalfOpenUtcWindows_ThatShareTheirBoundaryWithoutOverlapping()
    {
        var ventanas = DashboardDias.Ventanas(HoyLima, 7);

        _ = ventanas.Should().OnlyContain(v => v.DesdeUtc.Kind == DateTimeKind.Utc && v.HastaUtc.Kind == DateTimeKind.Utc);
        _ = ventanas.Should().OnlyContain(v => v.HastaUtc - v.DesdeUtc == TimeSpan.FromDays(1));
        for (var i = 1; i < ventanas.Count; i++)
        {
            _ = ventanas[i].DesdeUtc.Should().Be(ventanas[i - 1].HastaUtc);
        }
    }

    [Fact]
    public void Ventanas_ShouldStartTheLocalDayAtItsUtcInstant_WhenTheOffsetIsLima()
    {
        var hoy = DashboardDias.Ventanas(HoyLima, 7)[^1];

        _ = hoy.DesdeUtc.Should().Be(new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc));
        _ = hoy.HastaUtc.Should().Be(new DateTime(2026, 10, 3, 5, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Ventana_ShouldReturnOnlyToday()
    {
        var hoy = DashboardDias.Ventana(HoyLima);

        _ = hoy.Should().Be(new VentanaDia(HoyLima, HoyLima.UtcDateTime, HoyLima.AddDays(1).UtcDateTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-36)]
    [InlineData(36)]
    [InlineData(-19)]
    public void EsHoyValido_ShouldAccept_WhenHoyIsWithinTheToleranceOfNow(double horas)
    {
        var ahora = new DateTimeOffset(2026, 10, 2, 15, 30, 0, TimeSpan.Zero);

        _ = DashboardDias.EsHoyValido(ahora.AddHours(horas), ahora).Should().BeTrue();
    }

    [Theory]
    [InlineData(-36.1)]
    [InlineData(36.1)]
    [InlineData(-72)]
    [InlineData(24 * 365)]
    public void EsHoyValido_ShouldReject_WhenHoyIsFurtherThanTheToleranceFromNow(double horas)
    {
        var ahora = new DateTimeOffset(2026, 10, 2, 15, 30, 0, TimeSpan.Zero);

        _ = DashboardDias.EsHoyValido(ahora.AddHours(horas), ahora).Should().BeFalse();
    }

    [Fact]
    public void EsHoyValido_ShouldReject_WhenHoyWasNotSent() =>
        _ = DashboardDias.EsHoyValido(default, DateTimeOffset.UtcNow).Should().BeFalse();
}

using FluentAssertions;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Dashboard.Common;

public class PagosPorMetodoCalculatorTests
{
    private static readonly Guid VentaA = Guid.NewGuid();
    private static readonly Guid VentaB = Guid.NewGuid();

    [Fact]
    public void Calcular_ShouldReturnTheFourMethodsAtZero_WhenThereAreNoSales()
    {
        var totales = PagosPorMetodoCalculator.Calcular([], []);

        _ = totales.Keys.Should().BeEquivalentTo(Enum.GetValues<MetodoPago>());
        _ = totales.Values.Should().OnlyContain(monto => monto == 0m);
    }

    [Fact]
    public void Calcular_ShouldCountCashNetOfChange_WhenTheCustomerHandsOverMoreThanTheTotal()
    {
        var totales = PagosPorMetodoCalculator.Calcular(
            [new VentaCobrada(VentaA, 2.5m)],
            [new PagoCobrado(VentaA, MetodoPago.Efectivo, 10m)]);

        _ = totales[MetodoPago.Efectivo].Should().Be(2.5m);
    }

    [Fact]
    public void Calcular_ShouldTakeTheChangeOnlyFromCash_WhenTheSaleMixesMethods()
    {
        var totales = PagosPorMetodoCalculator.Calcular(
            [new VentaCobrada(VentaA, 40m)],
            [new PagoCobrado(VentaA, MetodoPago.YapePlin, 15m), new PagoCobrado(VentaA, MetodoPago.Efectivo, 30m)]);

        _ = totales[MetodoPago.YapePlin].Should().Be(15m);
        _ = totales[MetodoPago.Efectivo].Should().Be(25m);
        _ = totales.Values.Sum().Should().Be(40m);
    }

    [Fact]
    public void Calcular_ShouldComputeTheChangeAgainstTheAmountDue_WhenInsuranceCoversPartOfTheSale()
    {
        // Gross 100 with 60 covered leaves 40 to pay; 50 in cash gives 10 of change
        var totales = PagosPorMetodoCalculator.Calcular(
            [new VentaCobrada(VentaA, 40m)],
            [new PagoCobrado(VentaA, MetodoPago.Efectivo, 50m)]);

        _ = totales[MetodoPago.Efectivo].Should().Be(40m);
    }

    [Fact]
    public void Calcular_ShouldAddUpEachMethodAcrossSales_KeepingTheChangeOfEachSaleApart()
    {
        var totales = PagosPorMetodoCalculator.Calcular(
            [new VentaCobrada(VentaA, 20m), new VentaCobrada(VentaB, 30m)],
            [
                new PagoCobrado(VentaA, MetodoPago.Efectivo, 50m),
                new PagoCobrado(VentaB, MetodoPago.Tarjeta, 30m),
            ]);

        _ = totales[MetodoPago.Efectivo].Should().Be(20m);
        _ = totales[MetodoPago.Tarjeta].Should().Be(30m);
        _ = totales[MetodoPago.YapePlin].Should().Be(0m);
        _ = totales[MetodoPago.Transferencia].Should().Be(0m);
    }

    [Fact]
    public void Calcular_ShouldIgnorePayments_WhenTheirSaleIsNotAmongTheCountedOnes()
    {
        var totales = PagosPorMetodoCalculator.Calcular(
            [new VentaCobrada(VentaA, 20m)],
            [new PagoCobrado(VentaA, MetodoPago.Efectivo, 20m), new PagoCobrado(VentaB, MetodoPago.Efectivo, 99m)]);

        _ = totales[MetodoPago.Efectivo].Should().Be(20m);
    }

    [Fact]
    public void Calcular_ShouldNotGoNegative_WhenASaleHasNoPayments()
    {
        var totales = PagosPorMetodoCalculator.Calcular([new VentaCobrada(VentaA, 60m)], []);

        _ = totales.Values.Should().OnlyContain(monto => monto == 0m);
    }
}

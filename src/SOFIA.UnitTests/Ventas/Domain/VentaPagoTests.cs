using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Domain;

public class VentaPagoTests
{
    [Theory]
    [InlineData(MetodoPago.Tarjeta, null)]
    [InlineData(MetodoPago.Tarjeta, "")]
    [InlineData(MetodoPago.YapePlin, "   ")]
    [InlineData(MetodoPago.Transferencia, null)]
    public void Create_ShouldFail_WhenNonCashPaymentHasNoReference(MetodoPago metodo, string? referencia)
    {
        var result = VentaPago.Create(metodo, 10, referencia, DateTime.UtcNow);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Pagos.ReferenciaRequerida");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenCashPaymentHasNoReference()
    {
        var result = VentaPago.Create(MetodoPago.Efectivo, 10, null, DateTime.UtcNow);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.ReferenciaOperacion.Should().BeNull();
    }

    [Theory]
    [InlineData(MetodoPago.YapePlin)]
    [InlineData(MetodoPago.Tarjeta)]
    [InlineData(MetodoPago.Transferencia)]
    public void Create_ShouldSucceed_WhenNonCashPaymentHasReference(MetodoPago metodo)
    {
        var result = VentaPago.Create(metodo, 10, "OP-123456", DateTime.UtcNow);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.ReferenciaOperacion.Should().Be("OP-123456");
    }

    [Fact]
    public void Update_ShouldFail_WhenChangedToNonCashWithoutReference()
    {
        var pago = VentaPago.Create(MetodoPago.Efectivo, 10, null, DateTime.UtcNow).Value!;

        var result = pago.Update(MetodoPago.Tarjeta, 10, null, DateTime.UtcNow);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Pagos.ReferenciaRequerida");
        _ = pago.MetodoPago.Should().Be(MetodoPago.Efectivo);
    }

    [Theory]
    [InlineData(MetodoPago.Efectivo, false)]
    [InlineData(MetodoPago.YapePlin, true)]
    [InlineData(MetodoPago.Tarjeta, true)]
    [InlineData(MetodoPago.Transferencia, true)]
    public void RequiereReferencia_ShouldBeTrueOnlyForNonCashMethods(MetodoPago metodo, bool esperado) =>
        VentaPago.RequiereReferencia(metodo).Should().Be(esperado);
}

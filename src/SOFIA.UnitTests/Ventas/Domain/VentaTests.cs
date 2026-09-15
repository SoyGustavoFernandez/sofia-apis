using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Domain;

public class VentaTests
{
    private static readonly Guid SucursalId = Guid.NewGuid();
    private static readonly Guid EmpleadoId = Guid.NewGuid();

    private static Venta CrearVenta(EstadoVenta estado = EstadoVenta.Completada)
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 1, 10, 5).Value!;
        return Venta.Create(SucursalId, EmpleadoId, null, Guid.NewGuid(), [detalle], estado).Value!;
    }

    [Fact]
    public void Create_ShouldDefaultToCompletada_WhenEstadoIsNotSpecified()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 1, 10, 5).Value!;
        var result = Venta.Create(SucursalId, EmpleadoId, null, Guid.NewGuid(), [detalle]);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Estado.Should().Be(EstadoVenta.Completada);
    }

    [Fact]
    public void Create_ShouldAllowPendienteEstado_ForAHeldSale()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);

        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
        _ = venta.Pagos.Should().BeEmpty();
    }

    [Fact]
    public void RegistrarPagos_ShouldCompleteThePendingSale_WhenPaymentCoversTheTotal()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        var pago = VentaPago.Create(MetodoPago.Efectivo, 10, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([pago]);

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = venta.Pagos.Should().ContainSingle();
    }

    [Fact]
    public void RegistrarPagos_ShouldFail_WhenVentaIsAlreadyAnulada()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        _ = venta.Anular("Cliente nunca volvió");
        var pago = VentaPago.Create(MetodoPago.Efectivo, 10, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([pago]);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
        _ = venta.Estado.Should().Be(EstadoVenta.Anulada);
    }

    [Fact]
    public void RegistrarPagos_ShouldFail_WhenSumOfPaymentsIsInsufficient()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        var pago = VentaPago.Create(MetodoPago.Efectivo, 5, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([pago]);

        _ = result.IsFailure.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
    }

    [Fact]
    public void ActualizarDetalles_ShouldReplaceItemsAndRecalculateTotal_WhenVentaIsPendiente()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente); // total = 10 (1 x 10)
        var clienteId = Guid.NewGuid();
        var nuevosDetalles = new List<DetalleVenta> { DetalleVenta.Create(Guid.NewGuid(), 3, 8, 5).Value! };

        var result = venta.ActualizarDetalles(nuevosDetalles, clienteId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Detalles.Should().ContainSingle();
        _ = venta.MontoTotalBruto.Should().Be(24); // 3 x 8
        _ = venta.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public void ActualizarDetalles_ShouldFail_WhenVentaIsNotPendiente()
    {
        var venta = CrearVenta(EstadoVenta.Completada);
        var nuevosDetalles = new List<DetalleVenta> { DetalleVenta.Create(Guid.NewGuid(), 1, 10, 5).Value! };

        var result = venta.ActualizarDetalles(nuevosDetalles, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.ActualizarDetalles");
    }

    [Fact]
    public void ActualizarDetalles_ShouldFail_WhenNewDetallesIsEmpty()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);

        var result = venta.ActualizarDetalles([], null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Detalles");
    }
}

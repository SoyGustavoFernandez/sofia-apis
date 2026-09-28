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

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void RegistrarPagos_ShouldFail_WhenInsuranceCoverageIsOutOfBounds(decimal montoCubierto)
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        var pago = VentaPago.Create(MetodoPago.Efectivo, 10, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([pago], montoCubierto);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Seguro.MontoInvalido");
        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
        _ = venta.Pagos.Should().BeEmpty();
    }

    [Fact]
    public void RegistrarPagos_ShouldOnlyRequireTheCopay_WhenCoverageIsWithinTheTotal()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        var pago = VentaPago.Create(MetodoPago.Tarjeta, 3, "AUTH-1", DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([pago], 7);

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
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

    [Fact]
    public void AsignarSesion_ShouldMoveThePendingSaleToTheGivenSession()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        var sesionId = Guid.NewGuid();

        var result = venta.AsignarSesion(sesionId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.SesionId.Should().Be(sesionId);
    }

    [Fact]
    public void AsignarSesion_ShouldFail_WhenVentaIsNotPendiente()
    {
        var venta = CrearVenta(EstadoVenta.Completada);
        var sesionOriginal = venta.SesionId;

        var result = venta.AsignarSesion(Guid.NewGuid());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Caja");
        _ = venta.SesionId.Should().Be(sesionOriginal);
    }

    [Fact]
    public void RegistrarPagos_ShouldFail_WhenChangeExceedsTheCashReceived()
    {
        var venta = Venta.Create(SucursalId, EmpleadoId, null, Guid.NewGuid(), [DetalleVenta.Create(Guid.NewGuid(), 1, 50, 5).Value!], EstadoVenta.Pendiente).Value!;
        var tarjeta = VentaPago.Create(MetodoPago.Tarjeta, 100, "AUTH-1", DateTime.UtcNow).Value!;
        var efectivo = VentaPago.Create(MetodoPago.Efectivo, 5, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([tarjeta, efectivo]); // change 55 > cash 5

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Pagos.VueltoExcedeEfectivo");
        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
        _ = venta.Pagos.Should().BeEmpty();
    }

    [Fact]
    public void RegistrarPagos_ShouldAllowChange_WhenItIsCoveredByTheCashReceived()
    {
        var venta = Venta.Create(SucursalId, EmpleadoId, null, Guid.NewGuid(), [DetalleVenta.Create(Guid.NewGuid(), 1, 50, 5).Value!], EstadoVenta.Pendiente).Value!;
        var tarjeta = VentaPago.Create(MetodoPago.Tarjeta, 30, "AUTH-1", DateTime.UtcNow).Value!;
        var efectivo = VentaPago.Create(MetodoPago.Efectivo, 40, null, DateTime.UtcNow).Value!;

        var result = venta.RegistrarPagos([tarjeta, efectivo]); // change 20 <= cash 40

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
    }

    [Fact]
    public void Anular_ShouldFail_WhenVentaIsDevuelta()
    {
        var venta = CrearVenta();
        var detalle = venta.Detalles.Single();
        _ = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalle.Id] = detalle.CantidadVendida }, new Dictionary<Guid, decimal>());

        var result = venta.Anular("Cliente");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.ConDevoluciones");
        _ = venta.Estado.Should().Be(EstadoVenta.Devuelta);
    }

    private static Venta CrearVentaDosLineas(out DetalleVenta linea1, out DetalleVenta linea2)
    {
        linea1 = DetalleVenta.Create(Guid.NewGuid(), 5, 10, 5).Value!;
        linea2 = DetalleVenta.Create(Guid.NewGuid(), 2, 3.335m, 1).Value!;
        return Venta.Create(SucursalId, EmpleadoId, null, Guid.NewGuid(), [linea1, linea2]).Value!;
    }

    [Fact]
    public void RegistrarDevolucion_ShouldReturnTheCreditedAmount_AndKeepCompletada_WhenPartial()
    {
        var venta = CrearVentaDosLineas(out var linea1, out var linea2);

        var result = venta.RegistrarDevolucion(
            new Dictionary<Guid, decimal> { [linea1.Id] = 2, [linea2.Id] = 1 },
            new Dictionary<Guid, decimal> { [linea1.Id] = 1 });

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(23.34m); // 2 x 10 + 1 x 3.335 rounded
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
    }

    [Fact]
    public void RegistrarDevolucion_ShouldTouchTheSale_WhenPartialSoConcurrentReturnsConflictOnRowVersion()
    {
        var venta = CrearVentaDosLineas(out var linea1, out _);
        var antes = DateTimeOffset.UtcNow;

        var result = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [linea1.Id] = 1 }, new Dictionary<Guid, decimal>());

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = venta.LastModifiedAt.Should().NotBeNull().And.BeOnOrAfter(antes);
    }

    [Fact]
    public void RegistrarDevolucion_ShouldNotTouchTheSale_WhenRejected()
    {
        var venta = CrearVentaDosLineas(out var linea1, out _);

        var result = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [linea1.Id] = 99 }, new Dictionary<Guid, decimal>());

        _ = result.IsFailure.Should().BeTrue();
        _ = venta.LastModifiedAt.Should().BeNull();
    }

    [Fact]
    public void RegistrarDevolucion_ShouldMarkDevuelta_WhenEveryLineIsFullyReturned()
    {
        var venta = CrearVentaDosLineas(out var linea1, out var linea2);

        var result = venta.RegistrarDevolucion(
            new Dictionary<Guid, decimal> { [linea1.Id] = 3, [linea2.Id] = 2 },
            new Dictionary<Guid, decimal> { [linea1.Id] = 2 });

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Devuelta);
    }

    [Fact]
    public void RegistrarDevolucion_ShouldFail_WhenPreviousPlusRequestedExceedsSold()
    {
        var venta = CrearVentaDosLineas(out var linea1, out _);

        var result = venta.RegistrarDevolucion(
            new Dictionary<Guid, decimal> { [linea1.Id] = 2 },
            new Dictionary<Guid, decimal> { [linea1.Id] = 4 });

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Cantidad.Excedida");
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
    }

    [Fact]
    public void RegistrarDevolucion_ShouldFail_WhenLineDoesNotBelongToTheSale()
    {
        var venta = CrearVenta();

        var result = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [Guid.NewGuid()] = 1 }, new Dictionary<Guid, decimal>());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DetalleVenta.NotFound");
    }

    [Theory]
    [InlineData(EstadoVenta.Pendiente)]
    [InlineData(EstadoVenta.Anulada)]
    public void RegistrarDevolucion_ShouldFail_WhenVentaIsNotCompletada(EstadoVenta estado)
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);
        if (estado == EstadoVenta.Anulada)
        {
            _ = venta.Anular("Error");
        }

        var result = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [venta.Detalles.Single().Id] = 1 }, new Dictionary<Guid, decimal>());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Venta.EstadoInvalido");
    }

    [Fact]
    public void RegistrarDevolucion_ShouldFail_WhenVentaIsAlreadyDevuelta()
    {
        var venta = CrearVenta();
        var detalleId = venta.Detalles.Single().Id;
        _ = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalleId] = 1 }, new Dictionary<Guid, decimal>());

        var result = venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalleId] = 1 }, new Dictionary<Guid, decimal> { [detalleId] = 1 });

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Venta.EstadoInvalido");
    }

    [Fact]
    public void AsignarSesion_ShouldFail_WhenSesionIdIsEmpty()
    {
        var venta = CrearVenta(EstadoVenta.Pendiente);

        var result = venta.AsignarSesion(Guid.Empty);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Caja");
    }
}

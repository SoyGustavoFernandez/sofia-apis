using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Devoluciones.Domain;

public class DevolucionCabeceraTests
{
    private static DevolucionCabecera CrearDevolucion()
    {
        var detalle = DevolucionDetalle.Create(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta).Value!;
        return DevolucionCabecera.Create(Guid.NewGuid(), null, Guid.NewGuid(), "07", "Motivo", DateTime.UtcNow, [detalle]).Value!;
    }

    [Fact]
    public void RegistrarReembolso_ShouldRecordSessionAmountAndMethod()
    {
        var devolucion = CrearDevolucion();
        var sesionId = Guid.NewGuid();

        var result = devolucion.RegistrarReembolso(sesionId, 12.5m, MetodoPago.YapePlin);

        _ = result.IsSuccess.Should().BeTrue();
        _ = devolucion.SesionId.Should().Be(sesionId);
        _ = devolucion.MontoReembolsado.Should().Be(12.5m);
        _ = devolucion.MetodoReembolso.Should().Be(MetodoPago.YapePlin);
    }

    [Fact]
    public void RegistrarReembolso_ShouldFail_WhenSessionIsEmpty()
    {
        var devolucion = CrearDevolucion();

        var result = devolucion.RegistrarReembolso(Guid.Empty, 10m, MetodoPago.Efectivo);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DevolucionCabecera.SesionId");
        _ = devolucion.SesionId.Should().BeNull();
    }

    [Fact]
    public void RegistrarReembolso_ShouldFail_WhenAmountIsNegative()
    {
        var devolucion = CrearDevolucion();

        var result = devolucion.RegistrarReembolso(Guid.NewGuid(), -1m, MetodoPago.Efectivo);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DevolucionCabecera.MontoReembolsado");
        _ = devolucion.MontoReembolsado.Should().BeNull();
    }
}

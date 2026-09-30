using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Delivery.Domain;

public class DespachoDeliveryTests
{
    // Reaches the requested state only through allowed transitions, since every dispatch starts in Preparando
    private static DespachoDelivery Despacho(EstadoDespacho estado)
    {
        var despacho = DespachoDelivery.Create(Guid.NewGuid(), "Rappi", null, "Av. Uno 123", null, null).Value!;
        if (estado is EstadoDespacho.En_Camino or EstadoDespacho.Entregado)
        {
            _ = CambiarEstado(despacho, EstadoDespacho.En_Camino);
        }

        if (estado is EstadoDespacho.Entregado or EstadoDespacho.Devuelto)
        {
            _ = CambiarEstado(despacho, estado);
        }

        return despacho;
    }

    private static SOFIA.Domain.Common.Result CambiarEstado(DespachoDelivery d, EstadoDespacho nuevo) =>
        d.Update(d.PlataformaServicio, d.CodigoRastreo, nuevo, d.DireccionEntrega, d.RepartidorNombre, d.EvidenciaFotograficaUrl);

    [Theory]
    [InlineData(EstadoDespacho.Preparando, EstadoDespacho.En_Camino)]
    [InlineData(EstadoDespacho.Preparando, EstadoDespacho.Devuelto)]
    [InlineData(EstadoDespacho.En_Camino, EstadoDespacho.Entregado)]
    [InlineData(EstadoDespacho.En_Camino, EstadoDespacho.Devuelto)]
    public void Update_ShouldChangeState_WhenTransitionIsAllowed(EstadoDespacho actual, EstadoDespacho nuevo)
    {
        var despacho = Despacho(actual);

        var result = CambiarEstado(despacho, nuevo);

        _ = result.IsSuccess.Should().BeTrue();
        _ = despacho.EstadoDespacho.Should().Be(nuevo);
    }

    [Theory]
    [InlineData(EstadoDespacho.Entregado, EstadoDespacho.Preparando)]
    [InlineData(EstadoDespacho.Entregado, EstadoDespacho.Devuelto)]
    [InlineData(EstadoDespacho.Devuelto, EstadoDespacho.En_Camino)]
    [InlineData(EstadoDespacho.En_Camino, EstadoDespacho.Preparando)]
    [InlineData(EstadoDespacho.Preparando, EstadoDespacho.Entregado)]
    public void Update_ShouldRejectAndKeepState_WhenTransitionIsNotAllowed(EstadoDespacho actual, EstadoDespacho nuevo)
    {
        var despacho = Despacho(actual);

        var result = CambiarEstado(despacho, nuevo);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Delivery.Estado.TransicionInvalida");
        _ = despacho.EstadoDespacho.Should().Be(actual);
    }

    [Fact]
    public void Update_ShouldAllowEditingOtherFields_WhenStateIsUnchanged()
    {
        var despacho = Despacho(EstadoDespacho.Entregado);

        var result = despacho.Update("Glovo", "TRK-1", EstadoDespacho.Entregado, "Av. Dos 456", "Juan", null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = despacho.PlataformaServicio.Should().Be("Glovo");
    }

    [Fact]
    public void Create_ShouldStartInPreparando()
    {
        var result = DespachoDelivery.Create(Guid.NewGuid(), "Rappi", null, "Av. Uno 123", null, null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.EstadoDespacho.Should().Be(EstadoDespacho.Preparando);
    }
}

using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Devoluciones.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Devoluciones.Common;

public class CantidadesDevueltasExtensionsTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<DevolucionCabecera> _devoluciones = [];

    private DevolucionCabecera AgregarDevolucion(params (Guid DetalleVentaId, decimal Cantidad)[] lineas)
    {
        var detalles = lineas.Select(l => DevolucionDetalle.Create(l.DetalleVentaId, l.Cantidad, DestinoDevolucion.Reingreso_Venta).Value!).ToList();
        var devolucion = DevolucionCabecera.Create(Guid.NewGuid(), null, Guid.NewGuid(), "07", "Devolución", DateTime.UtcNow, detalles).Value!;
        _devoluciones.Add(devolucion);
        return devolucion;
    }

    private Task<Dictionary<Guid, decimal>> Ejecutar(params Guid[] detalleVentaIds)
    {
        _ = _dbContextMock.Setup(x => x.Devoluciones).Returns(_devoluciones.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.DetallesDevolucion).Returns(_devoluciones.SelectMany(d => d.Detalles).ToList().BuildMockDbSet().Object);
        return _dbContextMock.Object.GetCantidadesDevueltasAsync(detalleVentaIds, CancellationToken.None);
    }

    [Fact]
    public async Task GetCantidadesDevueltasAsync_WhenLinesWereReturnedSeveralTimes_ShouldSumPerLine()
    {
        var lineaA = Guid.NewGuid();
        var lineaB = Guid.NewGuid();
        _ = AgregarDevolucion((lineaA, 1m), (lineaB, 2m));
        _ = AgregarDevolucion((lineaA, 1.5m));

        var result = await Ejecutar(lineaA, lineaB);

        _ = result.Should().HaveCount(2);
        _ = result[lineaA].Should().Be(2.5m);
        _ = result[lineaB].Should().Be(2m);
    }

    [Fact]
    public async Task GetCantidadesDevueltasAsync_WhenLinesBelongToAnotherSale_ShouldIgnoreThem()
    {
        var lineaVenta = Guid.NewGuid();
        _ = AgregarDevolucion((Guid.NewGuid(), 3m));

        var result = await Ejecutar(lineaVenta);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCantidadesDevueltasAsync_WhenReturnOrLineIsSoftDeleted_ShouldIgnoreIt()
    {
        var linea = Guid.NewGuid();
        AgregarDevolucion((linea, 1m)).IsDeleted = true;
        AgregarDevolucion((linea, 2m)).Detalles.First().IsDeleted = true;
        _ = AgregarDevolucion((linea, 4m));

        var result = await Ejecutar(linea);

        _ = result[linea].Should().Be(4m);
    }
}

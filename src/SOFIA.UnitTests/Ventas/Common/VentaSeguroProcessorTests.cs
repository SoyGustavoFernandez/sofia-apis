using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Common;

public class VentaSeguroProcessorTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<VentaReclamoSeguro> _reclamosList = [];

    public VentaSeguroProcessorTests()
    {
        var reclamosDbSetMock = _reclamosList.BuildMockDbSet();
        _ = reclamosDbSetMock.Setup(d => d.Add(It.IsAny<VentaReclamoSeguro>())).Callback<VentaReclamoSeguro>(_reclamosList.Add);
        _ = _dbContextMock.Setup(c => c.VentasReclamosSeguro).Returns(reclamosDbSetMock.Object);
    }

    // Sale total = 2 x 10 = 20
    private static Venta CrearVenta()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 2, 10, 5).Value!;
        return Venta.Create(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), [detalle], EstadoVenta.Pendiente).Value!;
    }

    [Fact]
    public void Process_ShouldSkipClaim_WhenNoAseguradoraIsProvided()
    {
        // Act
        var result = VentaSeguroProcessor.Process(_dbContextMock.Object, CrearVenta(), null, 15);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = _reclamosList.Should().BeEmpty();
    }

    [Fact]
    public void Process_ShouldFail_WhenClaimCannotBeCreated()
    {
        // Act
        var result = VentaSeguroProcessor.Process(_dbContextMock.Object, CrearVenta(), Guid.NewGuid(), 25);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("VentaReclamoSeguro.MontoCopagoPaciente");
        _ = _reclamosList.Should().BeEmpty();
    }

    [Fact]
    public void Process_ShouldAddClaim_WhenCoverageIsValid()
    {
        // Act
        var result = VentaSeguroProcessor.Process(_dbContextMock.Object, CrearVenta(), Guid.NewGuid(), 15);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = _reclamosList.Should().ContainSingle().Which.MontoCopagoPaciente.Should().Be(5);
    }

    [Fact]
    public async Task ResolveCoberturaAsync_ShouldReturnZero_WhenNoAseguradoraIsProvided()
    {
        // Act
        var result = await VentaSeguroProcessor.ResolveCoberturaAsync(_dbContextMock.Object, null, 50, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(0);
    }

    [Fact]
    public async Task ResolveCoberturaAsync_ShouldReturnNotFound_WhenAseguradoraDoesNotExist()
    {
        // Arrange
        _ = _dbContextMock.Setup(c => c.Aseguradoras).Returns(new List<AseguradoraMedica>().BuildMockDbSet().Object);

        // Act
        var result = await VentaSeguroProcessor.ResolveCoberturaAsync(_dbContextMock.Object, Guid.NewGuid(), 50, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Aseguradora.NotFound");
    }
}

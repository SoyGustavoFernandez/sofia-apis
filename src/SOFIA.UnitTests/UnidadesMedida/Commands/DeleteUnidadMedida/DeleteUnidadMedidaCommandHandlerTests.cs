using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.UnidadesMedida.Commands.DeleteUnidadMedida;

public class DeleteUnidadMedidaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<UnidadMedida>> _unidadesMock;
    private readonly DeleteUnidadMedidaCommandHandler _handler;

    public DeleteUnidadMedidaCommandHandlerTests()
    {
        _unidadesMock = new List<UnidadMedida>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(_unidadesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupMedicamentos();
        SetupJerarquias();
        SetupFormulaciones();
        _handler = new DeleteUnidadMedidaCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(UnidadMedida? unidad) =>
        _unidadesMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(unidad));

    private void SetupMedicamentos(params Medicamento[] medicamentos) =>
        _dbContextMock.Setup(c => c.Medicamentos).Returns(medicamentos.ToList().BuildMockDbSet().Object);

    private void SetupJerarquias(params JerarquiaUoM[] jerarquias) =>
        _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(jerarquias.ToList().BuildMockDbSet().Object);

    private void SetupFormulaciones(params FormulacionClinica[] formulaciones) =>
        _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(formulaciones.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUnidadDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(new DeleteUnidadMedidaCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByMedicamento()
    {
        var unidad = UnidadMedida.Create("TAB", "Tableta").Value!;
        SetupFind(unidad);
        SetupMedicamentos(Medicamento.Create("COD-001", "Aspirina", Guid.NewGuid(), unidad.Id, CondicionVenta.VentaLibreOTC).Value!);

        var result = await _handler.Handle(new DeleteUnidadMedidaCommand(unidad.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.InUse");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByJerarquia()
    {
        var unidad = UnidadMedida.Create("TAB", "Tableta").Value!;
        SetupFind(unidad);
        SetupJerarquias(JerarquiaUoM.Create(Guid.NewGuid(), unidad.Id, Guid.NewGuid(), 10m).Value!);

        var result = await _handler.Handle(new DeleteUnidadMedidaCommand(unidad.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.InUse");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByFormulacionClinica()
    {
        var unidad = UnidadMedida.Create("MG", "Miligramo").Value!;
        SetupFind(unidad);
        SetupFormulaciones(FormulacionClinica.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, unidad.Id, null).Value!);

        var result = await _handler.Handle(new DeleteUnidadMedidaCommand(unidad.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.InUse");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenNotReferenced()
    {
        var unidad = UnidadMedida.Create("TAB", "Tableta").Value!;
        SetupFind(unidad);

        var result = await _handler.Handle(new DeleteUnidadMedidaCommand(unidad.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _unidadesMock.Verify(m => m.Remove(unidad), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

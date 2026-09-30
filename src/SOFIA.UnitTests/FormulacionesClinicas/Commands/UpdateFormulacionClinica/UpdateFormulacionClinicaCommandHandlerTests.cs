using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.FormulacionesClinicas.Commands.Update;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.FormulacionesClinicas.Commands.UpdateFormulacionClinica;

public class UpdateFormulacionClinicaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly UpdateFormulacionClinicaCommandHandler _handler;

    private readonly IngredienteActivo _ingrediente = IngredienteActivo.Create("Espironolactona", "C03DA01").Value!;
    private readonly UnidadMedida _unidadMedida = UnidadMedida.Create("MG", "Miligramo").Value!;

    public UpdateFormulacionClinicaCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupIngredientes(_ingrediente);
        SetupUnidadesMedida(_unidadMedida);
        _handler = new UpdateFormulacionClinicaCommandHandler(_dbContextMock.Object);
    }

    private void SetupFormulaciones(params FormulacionClinica[] items) =>
        _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(items.ToList().BuildMockDbSet().Object);

    private void SetupIngredientes(params IngredienteActivo[] items) =>
        _dbContextMock.Setup(c => c.IngredientesActivos).Returns(items.ToList().BuildMockDbSet().Object);

    private void SetupUnidadesMedida(params UnidadMedida[] items) =>
        _dbContextMock.Setup(c => c.UnidadesMedida).Returns(items.ToList().BuildMockDbSet().Object);

    private FormulacionClinica NewFormulacion() =>
        FormulacionClinica.Create(Guid.NewGuid(), _ingrediente.Id, 100m, _unidadMedida.Id, "TE-045").Value!;

    private UpdateFormulacionClinicaCommand Command(Guid id) => new()
    {
        Id = id,
        IngredienteId = _ingrediente.Id,
        ConcentracionDosis = 250m,
        UnidadMedidaId = _unidadMedida.Id,
        CodigoTeOrange = "TE-050",
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenFormulacionDoesNotExist()
    {
        SetupFormulaciones();

        var result = await _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenCommandValid()
    {
        var formulacion = NewFormulacion();
        SetupFormulaciones(formulacion);

        var result = await _handler.Handle(Command(formulacion.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = formulacion.ConcentracionDosis.Should().Be(250m);
        _ = formulacion.CodigoTeOrange.Should().Be("TE-050");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNewIngredienteDoesNotExist()
    {
        var formulacion = NewFormulacion();
        SetupFormulaciones(formulacion);
        SetupIngredientes();

        var command = Command(formulacion.Id) with { IngredienteId = Guid.NewGuid() };
        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNewUnidadMedidaDoesNotExist()
    {
        var formulacion = NewFormulacion();
        SetupFormulaciones(formulacion);
        SetupUnidadesMedida();

        var command = Command(formulacion.Id) with { UnidadMedidaId = Guid.NewGuid() };
        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCheckExistence_WhenIngredienteAndUnidadUnchanged()
    {
        var formulacion = NewFormulacion();
        SetupFormulaciones(formulacion);
        SetupIngredientes();
        SetupUnidadesMedida();

        var command = Command(formulacion.Id) with { IngredienteId = formulacion.IngredienteId, UnidadMedidaId = formulacion.UnidadMedidaId };
        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var formulacion = NewFormulacion();
        SetupFormulaciones(formulacion);

        var command = Command(formulacion.Id) with { ConcentracionDosis = 0 };
        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.Concentracion");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenTheNewIngredientIsAlreadyListedForTheProduct()
    {
        var otroIngrediente = IngredienteActivo.Create("Furosemida", "C03CA01").Value!;
        SetupIngredientes(_ingrediente, otroIngrediente);
        var formulacion = NewFormulacion();
        var otra = FormulacionClinica.Create(formulacion.ProductoId, otroIngrediente.Id, 40m, _unidadMedida.Id, null).Value!;
        SetupFormulaciones(formulacion, otra);

        var result = await _handler.Handle(Command(formulacion.Id) with { IngredienteId = otroIngrediente.Id }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Formulacion.Ingrediente.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenTheNewIngredientOnlyBelongsToADeletedFormulacion()
    {
        var otroIngrediente = IngredienteActivo.Create("Furosemida", "C03CA01").Value!;
        SetupIngredientes(_ingrediente, otroIngrediente);
        var formulacion = NewFormulacion();
        var eliminada = FormulacionClinica.Create(formulacion.ProductoId, otroIngrediente.Id, 40m, _unidadMedida.Id, null).Value!;
        eliminada.IsDeleted = true;
        SetupFormulaciones(formulacion, eliminada);

        var result = await _handler.Handle(Command(formulacion.Id) with { IngredienteId = otroIngrediente.Id }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}

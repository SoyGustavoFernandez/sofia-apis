using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.FormulacionesClinicas.Commands.Create;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.FormulacionesClinicas.Commands.CreateFormulacionClinica;

public class CreateFormulacionClinicaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<FormulacionClinica>> _formulacionesMock;
    private readonly CreateFormulacionClinicaCommandHandler _handler;

    private readonly Medicamento _medicamento = Medicamento.Create(
        "COD-001", "Aldactone 100mg", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaSimple).Value!;
    private readonly IngredienteActivo _ingrediente = IngredienteActivo.Create("Espironolactona", "C03DA01").Value!;
    private readonly UnidadMedida _unidadMedida = UnidadMedida.Create("MG", "Miligramo").Value!;

    public CreateFormulacionClinicaCommandHandlerTests()
    {
        _formulacionesMock = new List<FormulacionClinica>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(_formulacionesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupMedicamentos(_medicamento);
        SetupIngredientes(_ingrediente);
        SetupUnidadesMedida(_unidadMedida);
        _handler = new CreateFormulacionClinicaCommandHandler(_dbContextMock.Object);
    }

    private void SetupMedicamentos(params Medicamento[] items) =>
        _dbContextMock.Setup(c => c.Medicamentos).Returns(items.ToList().BuildMockDbSet().Object);

    private void SetupIngredientes(params IngredienteActivo[] items) =>
        _dbContextMock.Setup(c => c.IngredientesActivos).Returns(items.ToList().BuildMockDbSet().Object);

    private void SetupUnidadesMedida(params UnidadMedida[] items) =>
        _dbContextMock.Setup(c => c.UnidadesMedida).Returns(items.ToList().BuildMockDbSet().Object);

    private CreateFormulacionClinicaCommand ValidCommand() => new()
    {
        ProductoId = _medicamento.Id,
        IngredienteId = _ingrediente.Id,
        ConcentracionDosis = 100m,
        UnidadMedidaId = _unidadMedida.Id,
        CodigoTeOrange = "TE-045",
    };

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenCommandValid()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _ = result.Value.Should().NotBe(Guid.Empty);
        _formulacionesMock.Verify(m => m.Add(It.IsAny<FormulacionClinica>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMedicamentoDoesNotExist()
    {
        SetupMedicamentos();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIngredienteDoesNotExist()
    {
        SetupIngredientes();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUnidadMedidaDoesNotExist()
    {
        SetupUnidadesMedida();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var command = ValidCommand() with { ConcentracionDosis = 0 };

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.Concentracion");
        _formulacionesMock.Verify(m => m.Add(It.IsAny<FormulacionClinica>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

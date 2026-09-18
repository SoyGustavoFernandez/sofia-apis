using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.CreateMedicamento;

public class CreateMedicamentoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Medicamento>> _medicamentosMock;
    private readonly CreateMedicamentoCommandHandler _handler;

    public CreateMedicamentoCommandHandlerTests()
    {
        _medicamentosMock = new List<Medicamento>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateMedicamentoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenCommandValid()
    {
        var command = new CreateMedicamentoCommand("COD-001", "Paracetamol", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _ = result.Value.Should().NotBe(Guid.Empty);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var command = new CreateMedicamentoCommand("", "Paracetamol", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional");
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

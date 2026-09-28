using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Commands.CreateSerieFiscal;

public class CreateSerieFiscalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Sucursal _sucursal = Sucursal.Create("Central", "Av. Lima 123", "LIC-001").Value!;
    private readonly List<SunatSerieFiscal> _series = [];
    private readonly List<SunatSerieFiscal> _agregadas = [];

    private CreateSerieFiscalCommandHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursal }.BuildMockDbSet().Object);
        var seriesMock = _series.BuildMockDbSet();
        _ = seriesMock.Setup(d => d.Add(It.IsAny<SunatSerieFiscal>())).Callback<SunatSerieFiscal>(_agregadas.Add);
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(seriesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new CreateSerieFiscalCommandHandler(_dbContextMock.Object);
    }

    private CreateSerieFiscalCommand Command(TipoComprobante tipo = TipoComprobante.Boleta, string prefijo = "B001", string estado = SunatSerieFiscal.EstadoActiva) =>
        new(_sucursal.Id, tipo, prefijo, 0, estado);

    [Fact]
    public async Task Handle_ShouldCreateSeries_WhenNoConflicts()
    {
        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _ = _agregadas.Should().ContainSingle(s => s.PrefijoSerie == "B001" && s.SucursalId == _sucursal.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBranchDoesNotExistInTenant()
    {
        var command = Command() with { SucursalId = Guid.NewGuid() };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _agregadas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPrefixIsTakenForTheType()
    {
        _series.Add(SunatSerieFiscal.Create(Guid.NewGuid(), TipoComprobante.Boleta, "B001", 5, SunatSerieFiscal.EstadoInactiva).Value!);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.PrefijoDuplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _agregadas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldAllowSamePrefix_ForAnotherType()
    {
        _series.Add(SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.NotaCredito, "B001", 5, SunatSerieFiscal.EstadoActiva).Value!);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenBranchAlreadyHasAnActiveSeriesOfTheType()
    {
        _series.Add(SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.Boleta, "B002", 5, SunatSerieFiscal.EstadoActiva).Value!);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.ActivaDuplicada");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldAllowInactiveSeries_WhenBranchAlreadyHasAnActiveOne()
    {
        _series.Add(SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.Boleta, "B002", 5, SunatSerieFiscal.EstadoActiva).Value!);

        var result = await CreateHandler().Handle(Command(estado: SunatSerieFiscal.EstadoInactiva), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapUniqueIndexRace_ToConflict()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("save failed", new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.SUNAT_Series_Fiscales' with unique index 'UX_SUNAT_Series_Fiscales_Sucursal_Tipo_Activa'.")));

        var result = await handler.Handle(Command(), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.ActivaDuplicada");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldRethrow_WhenSaveFailsForAnotherReason()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("save failed", new InvalidOperationException("timeout")));

        var act = () => handler.Handle(Command(), CancellationToken.None);

        _ = await act.Should().ThrowAsync<DbUpdateException>();
    }
}

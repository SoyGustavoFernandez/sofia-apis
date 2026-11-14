using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;
using SOFIA.Domain.Entities;
using SOFIA.UnitTests.Plataforma.Domain;

namespace SOFIA.UnitTests.Plataforma.Commands;

public class GuardarConfiguracionPagoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GuardarConfiguracionPagoCommandHandler _handler;

    public GuardarConfiguracionPagoCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.EsAdminPlataforma).Returns(true);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new GuardarConfiguracionPagoCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private Mock<DbSet<PlataformaConfiguracionPago>> SetupConfiguraciones(params PlataformaConfiguracionPago[] existentes)
    {
        var set = existentes.ToList().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.PlataformaConfiguracionesPago).Returns(set.Object);
        return set;
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenAndSaveNothing_WhenCallerIsNotPlatformAdmin()
    {
        _ = _currentUserMock.Setup(u => u.EsAdminPlataforma).Returns(false);
        var set = SetupConfiguraciones();

        var result = await _handler.Handle(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321" }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(403);
        _ = result.Error.Code.Should().Be("Auth.PlataformaAdminRequerido");
        set.Verify(s => s.Add(It.IsAny<PlataformaConfiguracionPago>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateTheRow_WhenNoneExists()
    {
        var set = SetupConfiguraciones();
        PlataformaConfiguracionPago? creada = null;
        _ = set.Setup(s => s.Add(It.IsAny<PlataformaConfiguracionPago>())).Callback<PlataformaConfiguracionPago>(c => creada = c);

        var result = await _handler.Handle(
            new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", YapeNumero = "987654321", Instrucciones = "Plan mensual", QrImagen = PlataformaConfiguracionPagoTests.Png },
            CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = creada.Should().NotBeNull();
        _ = result.Value.Should().Be(creada.Id);
        _ = creada.WhatsappNumero.Should().Be("51987654321");
        _ = creada.YapeNumero.Should().Be("987654321");
        _ = creada.QrContentType.Should().Be("image/png");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTheExistingRowAndKeepItsQr_WhenNoImageIsSent()
    {
        var existente = PlataformaConfiguracionPago.Create("51987654321", "Antes").Value!;
        _ = existente.CambiarQr(PlataformaConfiguracionPagoTests.Png);
        var set = SetupConfiguraciones(existente);

        var result = await _handler.Handle(
            new GuardarConfiguracionPagoCommand { WhatsappNumero = "51999888777", Instrucciones = "Después" },
            CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(existente.Id);
        _ = existente.WhatsappNumero.Should().Be("51999888777");
        _ = existente.Instrucciones.Should().Be("Después");
        _ = existente.QrImagen.Should().Equal(PlataformaConfiguracionPagoTests.Png);
        set.Verify(s => s.Add(It.IsAny<PlataformaConfiguracionPago>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFailAndSaveNothing_WhenImageIsNotPngOrJpeg()
    {
        _ = SetupConfiguraciones();

        var result = await _handler.Handle(
            new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", QrImagen = "<svg></svg>"u8.ToArray() },
            CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.Qr.Formato");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, "QR sin cambio")]
    [InlineData(true, "QR reemplazado")]
    public void GetAuditEntry_ShouldRecordWhetherTheQrChanged(bool conQr, string detalle)
    {
        var id = Guid.NewGuid();
        var command = new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", QrImagen = conQr ? PlataformaConfiguracionPagoTests.Png : null };

        var entry = command.GetAuditEntry(id);

        _ = entry.Should().Be(new AuditEntry(AuditEventos.PagoConfigGuardar, AuditTablas.PlataformaConfiguracionPago, id, detalle));
    }
}

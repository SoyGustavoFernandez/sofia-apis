using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Common;

public class VentaDetalleFactoryTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _clienteId = Guid.NewGuid();
    private readonly Guid _loteRxId = Guid.NewGuid();
    private readonly Guid _loteOtcId = Guid.NewGuid();
    private readonly Guid _productoRxId = Guid.NewGuid();
    private readonly Guid _productoOtcId = Guid.NewGuid();
    private readonly List<InventarioSucursal> _inventario = [];
    private readonly List<Venta> _ventas = [];

    private LoteInventario _loteRx = null!;

    // Expiry stored like the SPA sends it (-05:00) for an expiry day that started the given hours ago.
    private static DateTimeOffset CaducidadIniciadaHace(double horas) => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).AddHours(-horas);

    private void SetupMocks(CondicionVenta condicionRx, List<RecetaMedica>? recetas = null, DateTimeOffset? caducidadRx = null)
    {
        _loteRx = LoteInventario.Create(_productoRxId, "L-RX", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        _loteRx.SetId(_loteRxId);
        if (caducidadRx.HasValue)
        {
            typeof(LoteInventario).GetProperty(nameof(LoteInventario.FechaCaducidad))!.SetValue(_loteRx, caducidadRx.Value);
        }

        var loteOtc = LoteInventario.Create(_productoOtcId, "L-OTC", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        loteOtc.SetId(_loteOtcId);

        var productoRx = Medicamento.Create("COD-RX", "Controlado", Guid.NewGuid(), Guid.NewGuid(), condicionRx, 10).Value!;
        productoRx.SetId(_productoRxId);
        var productoOtc = Medicamento.Create("COD-OTC", "Libre", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC, 5).Value!;
        productoOtc.SetId(_productoOtcId);

        _inventario.Add(InventarioSucursal.Create(_sucursalId, _loteRxId, 20).Value!);
        _inventario.Add(InventarioSucursal.Create(_sucursalId, _loteOtcId, 20).Value!);

        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { _loteRx, loteOtc }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { productoRx, productoOtc }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(_inventario.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DigemidInventarioCuarentena).Returns(new List<DigemidInventarioCuarentena>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.HistorialPreciosProveedor).Returns(new List<HistorialPrecioProveedor>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(new List<PresentacionVenta>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Recetas).Returns((recetas ?? []).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(_ventas.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DetallesVenta).Returns(_ventas.SelectMany(v => v.Detalles).ToList().BuildMockDbSet().Object);
    }

    private RecetaMedica CrearReceta(Guid? clienteId = null, int repeticionesMax = 0, string? numeroReceta = "RX-0001") =>
        RecetaMedica.Create(clienteId ?? _clienteId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), repeticionesMax, null, numeroReceta).Value!;

    private Venta RegistrarVentaPrevia(Guid loteId, Guid recetaId, EstadoVenta estado = EstadoVenta.Completada)
    {
        var venta = Venta.Create(_sucursalId, Guid.NewGuid(), _clienteId, Guid.NewGuid(), [DetalleVenta.Create(loteId, 1, 10, 5, recetaId).Value!], estado).Value!;
        _ventas.Add(venta);
        return venta;
    }

    private Task<Result<DetallesVentaConstruidos>> Build(List<CreateVentaDetailDto> dtos, Guid? clienteId, Guid? ventaIdExcluida = null, bool exigirRegistroReceta = false) =>
        VentaDetalleFactory.BuildAsync(_dbContextMock.Object, dtos, _sucursalId, clienteId, ventaIdExcluida, exigirRegistroReceta, CancellationToken.None);

    [Theory]
    [InlineData(24)]
    [InlineData(36)]
    public async Task BuildAsync_ShouldReturnVencido_WhenTheExpiryDayAlreadyEnded(double horas)
    {
        SetupMocks(CondicionVenta.VentaLibreOTC, caducidadRx: CaducidadIniciadaHace(horas));

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Lote.Vencido");
        _ = _inventario[0].CantidadFisica.Should().Be(20);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(23)]
    public async Task BuildAsync_ShouldSucceed_WhenTheLotExpiresToday(double horas)
    {
        SetupMocks(CondicionVenta.VentaLibreOTC, caducidadRx: CaducidadIniciadaHace(horas));

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _inventario[0].CantidadFisica.Should().Be(19);
    }

    [Fact]
    public async Task BuildAsync_ShouldSucceed_WhenOtcProductHasNoPrescription()
    {
        SetupMocks(CondicionVenta.Estupefaciente);

        var result = await Build([new CreateVentaDetailDto(_loteOtcId, 2)], null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Detalles.Single().RecetaId.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BuildAsync_ShouldNotRequireVerification_WhenSaleHasOnlyOtcLines(bool exigirRegistroReceta)
    {
        SetupMocks(CondicionVenta.RecetaSimple);

        var result = await Build([new CreateVentaDetailDto(_loteOtcId, 2)], null, exigirRegistroReceta: exigirRegistroReceta);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.RequiereVerificacionReceta.Should().BeFalse();
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaSimple, true)]
    [InlineData(CondicionVenta.RecetaRetenida, true)]
    [InlineData(CondicionVenta.RecetaRetenida, false)]
    [InlineData(CondicionVenta.Estupefaciente, true)]
    [InlineData(CondicionVenta.Estupefaciente, false)]
    public async Task BuildAsync_ShouldReturnRequerida_WhenPrescriptionProductHasNoPrescription(CondicionVenta condicion, bool exigirRegistroReceta)
    {
        SetupMocks(condicion);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1)], _clienteId, exigirRegistroReceta: exigirRegistroReceta);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Requerida");
        _ = _inventario[0].CantidadFisica.Should().Be(20);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BuildAsync_ShouldRequireVerification_WhenFlexibleCompanySellsSimplePrescriptionWithoutPrescription(bool conCliente)
    {
        SetupMocks(CondicionVenta.RecetaSimple);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1), new CreateVentaDetailDto(_loteOtcId, 1)], conCliente ? _clienteId : null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.RequiereVerificacionReceta.Should().BeTrue();
        _ = result.Value.Detalles.Should().OnlyContain(d => d.RecetaId == null);
        _ = _inventario[0].CantidadFisica.Should().Be(19);
    }

    [Fact]
    public async Task BuildAsync_ShouldNotRequireVerification_WhenFlexibleCompanyLinksARegisteredPrescription()
    {
        var receta = CrearReceta(numeroReceta: null);
        SetupMocks(CondicionVenta.RecetaSimple, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.RequiereVerificacionReceta.Should().BeFalse();
        _ = result.Value.Detalles.Single().RecetaId.Should().Be(receta.Id);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnOtroPaciente_WhenFlexibleCompanyLinksAForeignPrescription()
    {
        var recetaAjena = CrearReceta(clienteId: Guid.NewGuid());
        SetupMocks(CondicionVenta.RecetaSimple, [recetaAjena]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, recetaAjena.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.OtroPaciente");
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnAgotada_WhenFlexibleCompanyLinksAnExhaustedPrescription()
    {
        var receta = CrearReceta();
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id);
        SetupMocks(CondicionVenta.RecetaSimple, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Agotada");
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida, true, null)]
    [InlineData(CondicionVenta.RecetaRetenida, false, null)]
    [InlineData(CondicionVenta.Estupefaciente, true, null)]
    [InlineData(CondicionVenta.Estupefaciente, false, "   ")]
    public async Task BuildAsync_ShouldReturnNumeroRequerido_WhenControlledProductPrescriptionHasNoNumber(CondicionVenta condicion, bool exigirRegistroReceta, string? numeroReceta)
    {
        var receta = CrearReceta(numeroReceta: numeroReceta);
        SetupMocks(condicion, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId, exigirRegistroReceta: exigirRegistroReceta);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.NumeroRequerido");
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida, true)]
    [InlineData(CondicionVenta.RecetaRetenida, false)]
    [InlineData(CondicionVenta.Estupefaciente, true)]
    [InlineData(CondicionVenta.Estupefaciente, false)]
    public async Task BuildAsync_ShouldSucceedWithoutVerification_WhenControlledProductPrescriptionHasItsNumber(CondicionVenta condicion, bool exigirRegistroReceta)
    {
        var receta = CrearReceta(numeroReceta: "RE-778899");
        SetupMocks(condicion, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId, exigirRegistroReceta: exigirRegistroReceta);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.RequiereVerificacionReceta.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ExigeRegistroRecetaAsync_ShouldReturnTheCompanyFlag(bool flag, bool esperado)
    {
        EmisorMocks.Setup(_dbContextMock, _sucursalId, exigirRegistroReceta: flag);

        var exige = await VentaDetalleFactory.ExigeRegistroRecetaAsync(_dbContextMock.Object, _sucursalId, CancellationToken.None);

        _ = exige.Should().Be(esperado);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExigeRegistroRecetaAsync_ShouldBeStrict_WhenTheCompanyCannotBeResolved(bool sucursalSinEmpresa)
    {
        var sucursal = Sucursal.Create("Central", "Av. Arequipa 123", "LIC-1").Value!;
        sucursal.SetId(sucursalSinEmpresa ? _sucursalId : Guid.NewGuid());
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { sucursal }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(new List<Empresa> { Empresa.Create("Otra Botica").Value! }.BuildMockDbSet().Object);

        var exige = await VentaDetalleFactory.ExigeRegistroRecetaAsync(_dbContextMock.Object, _sucursalId, CancellationToken.None);

        _ = exige.Should().BeTrue();
    }

    private Task<Result<bool>> EvaluarGuardadas(bool exigirRegistroReceta, params DetalleVenta[] detalles) =>
        VentaDetalleFactory.RequiereVerificacionRecetaAsync(_dbContextMock.Object, detalles, exigirRegistroReceta, CancellationToken.None);

    [Fact]
    public async Task RequiereVerificacionRecetaAsync_ShouldRequireVerification_WhenFlexibleCompanyStoredSimplePrescriptionLineHasNoPrescription()
    {
        SetupMocks(CondicionVenta.RecetaSimple);

        var result = await EvaluarGuardadas(false, DetalleVenta.Create(_loteRxId, 1, 10, 5).Value!, DetalleVenta.Create(_loteOtcId, 1, 5, 2).Value!);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().BeTrue();
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaSimple, true)]
    [InlineData(CondicionVenta.RecetaRetenida, false)]
    [InlineData(CondicionVenta.Estupefaciente, false)]
    public async Task RequiereVerificacionRecetaAsync_ShouldReturnRequerida_WhenStoredLineNeedsARegisteredPrescription(CondicionVenta condicion, bool exigirRegistroReceta)
    {
        SetupMocks(condicion);

        var result = await EvaluarGuardadas(exigirRegistroReceta, DetalleVenta.Create(_loteRxId, 1, 10, 5).Value!);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Requerida");
    }

    [Fact]
    public async Task RequiereVerificacionRecetaAsync_ShouldReturnNumeroRequerido_WhenStoredControlledLinePrescriptionHasNoNumber()
    {
        var receta = CrearReceta(numeroReceta: null);
        SetupMocks(CondicionVenta.RecetaRetenida, [receta]);

        var result = await EvaluarGuardadas(false, DetalleVenta.Create(_loteRxId, 1, 10, 5, receta.Id).Value!);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.NumeroRequerido");
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida, "RE-778899")]
    [InlineData(CondicionVenta.RecetaSimple, null)]
    public async Task RequiereVerificacionRecetaAsync_ShouldNotRequireVerification_WhenStoredLinesAreCoveredByTheirPrescription(CondicionVenta condicion, string? numeroReceta)
    {
        var receta = CrearReceta(numeroReceta: numeroReceta);
        SetupMocks(condicion, [receta]);

        var result = await EvaluarGuardadas(false, DetalleVenta.Create(_loteRxId, 1, 10, 5, receta.Id).Value!, DetalleVenta.Create(_loteOtcId, 1, 5, 2).Value!);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task RequiereVerificacionRecetaAsync_ShouldReturnNoEncontrada_WhenStoredControlledLinePrescriptionNoLongerExists()
    {
        SetupMocks(CondicionVenta.Estupefaciente);

        var result = await EvaluarGuardadas(false, DetalleVenta.Create(_loteRxId, 1, 10, 5, Guid.NewGuid()).Value!);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.NoEncontrada");
    }

    [Fact]
    public async Task RequiereVerificacionRecetaAsync_ShouldReturnLoteNotFound_WhenStoredLineProductCannotBeResolved()
    {
        SetupMocks(CondicionVenta.RecetaSimple);

        var result = await EvaluarGuardadas(false, DetalleVenta.Create(Guid.NewGuid(), 1, 10, 5).Value!);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Lote");
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnNoEncontrada_WhenPrescriptionDoesNotExist()
    {
        SetupMocks(CondicionVenta.Estupefaciente);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, Guid.NewGuid())], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.NoEncontrada");
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnOtroPaciente_WhenPrescriptionBelongsToAnotherClient()
    {
        var recetaAjena = CrearReceta(clienteId: Guid.NewGuid());
        SetupMocks(CondicionVenta.RecetaRetenida, [recetaAjena]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, recetaAjena.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.OtroPaciente");
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnOtroPaciente_WhenSaleHasNoClient()
    {
        var receta = CrearReceta();
        SetupMocks(CondicionVenta.RecetaRetenida, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.OtroPaciente");
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnOtroPaciente_WhenOtcLineCarriesAForeignPrescription()
    {
        var recetaAjena = CrearReceta(clienteId: Guid.NewGuid());
        SetupMocks(CondicionVenta.Estupefaciente, [recetaAjena]);

        var result = await Build([new CreateVentaDetailDto(_loteOtcId, 1, recetaAjena.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.OtroPaciente");
    }

    [Fact]
    public async Task BuildAsync_ShouldSucceed_WhenControlledProductHasAnUnusedPrescription()
    {
        var receta = CrearReceta();
        SetupMocks(CondicionVenta.Estupefaciente, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Detalles.Single().RecetaId.Should().Be(receta.Id);
        _ = _inventario[0].CantidadFisica.Should().Be(19);
    }

    [Fact]
    public async Task BuildAsync_ShouldCountOneDispensation_WhenSeveralLinesShareThePrescription()
    {
        var receta = CrearReceta();
        SetupMocks(CondicionVenta.RecetaRetenida, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id), new CreateVentaDetailDto(_loteRxId, 2, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Detalles.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(CondicionVenta.RecetaRetenida)]
    [InlineData(CondicionVenta.Estupefaciente)]
    public async Task BuildAsync_ShouldReturnAgotada_WhenControlledPrescriptionWasAlreadyDispensed(CondicionVenta condicion)
    {
        var receta = CrearReceta(repeticionesMax: 5);
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id);
        SetupMocks(condicion, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Agotada");
    }

    [Fact]
    public async Task BuildAsync_ShouldIgnoreVoidedSales_WhenCountingDispensations()
    {
        var receta = CrearReceta();
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id, EstadoVenta.Anulada);
        SetupMocks(CondicionVenta.Estupefaciente, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_ShouldIgnoreThePendingSaleBeingReplaced_WhenCountingDispensations()
    {
        var receta = CrearReceta();
        var pendiente = RegistrarVentaPrevia(_loteRxId, receta.Id, EstadoVenta.Pendiente);
        SetupMocks(CondicionVenta.Estupefaciente, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId, pendiente.Id);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_ShouldIgnorePreviousOtcOnlyUses_WhenCountingDispensations()
    {
        var receta = CrearReceta();
        _ = RegistrarVentaPrevia(_loteOtcId, receta.Id);
        SetupMocks(CondicionVenta.Estupefaciente, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_ShouldAllowRefills_WhenSimplePrescriptionHasRepetitionsLeft()
    {
        var receta = CrearReceta(repeticionesMax: 1);
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id);
        SetupMocks(CondicionVenta.RecetaSimple, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnAgotada_WhenSimplePrescriptionUsedAllRepetitions()
    {
        var receta = CrearReceta(repeticionesMax: 1);
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id);
        _ = RegistrarVentaPrevia(_loteRxId, receta.Id);
        SetupMocks(CondicionVenta.RecetaSimple, [receta]);

        var result = await Build([new CreateVentaDetailDto(_loteRxId, 1, receta.Id)], _clienteId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Receta.Agotada");
    }
}

using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.POS.Domain;

public class PosSesionCajaTests
{
    private static readonly DateTime Apertura = new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    private static PosSesionCaja CreateAbierta() =>
        PosSesionCaja.Create(Guid.NewGuid(), Guid.NewGuid(), Apertura, 500m).Value!;

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsAreValid()
    {
        var result = PosSesionCaja.Create(Guid.NewGuid(), Guid.NewGuid(), Apertura, 500m);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.EstadoSesion.Should().Be(EstadoSesion.Abierta);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenMontoAperturaIsZero()
    {
        var result = PosSesionCaja.Create(Guid.NewGuid(), Guid.NewGuid(), Apertura, 0m);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenSucursalIdIsEmpty()
    {
        var result = PosSesionCaja.Create(Guid.Empty, Guid.NewGuid(), Apertura, 100m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.SucursalId");
    }

    [Fact]
    public void Create_ShouldFail_WhenEmpleadoIdIsEmpty()
    {
        var result = PosSesionCaja.Create(Guid.NewGuid(), Guid.Empty, Apertura, 100m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.EmpleadoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenMontoAperturaIsNegative()
    {
        var result = PosSesionCaja.Create(Guid.NewGuid(), Guid.NewGuid(), Apertura, -1m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.MontoAperturaEfectivo");
    }

    // ── Cerrar ───────────────────────────────────────────────────────────────

    [Fact]
    public void Cerrar_ShouldSetEstadoCuadrada_WhenArqueoIsZero()
    {
        var sesion = CreateAbierta();
        var cierre = Apertura.AddHours(8);

        var result = sesion.Cerrar(cierre, montoDeclarado: 800m, montoCalculado: 800m);

        _ = result.IsSuccess.Should().BeTrue();
        _ = sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
        _ = sesion.DiferenciaArqueo.Should().Be(0m);
    }

    [Fact]
    public void Cerrar_ShouldSetEstadoCerrada_WhenArqueoIsNotZero()
    {
        var sesion = CreateAbierta();
        var cierre = Apertura.AddHours(8);

        var result = sesion.Cerrar(cierre, montoDeclarado: 850m, montoCalculado: 800m);

        _ = result.IsSuccess.Should().BeTrue();
        _ = sesion.EstadoSesion.Should().Be(EstadoSesion.Cerrada);
        _ = sesion.DiferenciaArqueo.Should().Be(50m);
    }

    [Fact]
    public void Cerrar_ShouldCalculateDiferenciaArqueoCorrectly()
    {
        var sesion = CreateAbierta();
        var cierre = Apertura.AddHours(8);

        _ = sesion.Cerrar(cierre, montoDeclarado: 750m, montoCalculado: 800m);

        _ = sesion.DiferenciaArqueo.Should().Be(-50m);
    }

    [Fact]
    public void Cerrar_ShouldFail_WhenEstadoIsNotAbierta()
    {
        var sesion = CreateAbierta();
        var cierre = Apertura.AddHours(8);
        _ = sesion.Cerrar(cierre, 800m, 800m); // now Cuadrada

        var result = sesion.Cerrar(cierre.AddHours(1), 800m, 800m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.Cerrar");
    }

    [Fact]
    public void Cerrar_ShouldFail_WhenFechaCierreIsBeforeFechaApertura()
    {
        var sesion = CreateAbierta();

        var result = sesion.Cerrar(Apertura.AddSeconds(-1), 800m, 800m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.FechaHoraCierre");
    }

    [Fact]
    public void Cerrar_ShouldFail_WhenMontoDeclaradoIsNegative()
    {
        var sesion = CreateAbierta();

        var result = sesion.Cerrar(Apertura.AddHours(8), montoDeclarado: -1m, montoCalculado: 800m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.MontoCierreDeclarado");
    }

    [Fact]
    public void Cerrar_ShouldFail_WhenMontoCalculadoIsNegative()
    {
        var sesion = CreateAbierta();

        var result = sesion.Cerrar(Apertura.AddHours(8), montoDeclarado: 800m, montoCalculado: -1m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.MontoCierreCalculado");
    }
}

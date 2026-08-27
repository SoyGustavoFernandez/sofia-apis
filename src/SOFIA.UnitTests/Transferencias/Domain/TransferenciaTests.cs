using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Transferencias.Domain;

public class TransferenciaTests
{
    private readonly Guid _origenId  = Guid.NewGuid();
    private readonly Guid _destinoId = Guid.NewGuid();
    private readonly Guid _emisorId  = Guid.NewGuid();

    private List<DetalleTransferencia> ValidDetalles(int count = 1)
    {
        var detalles = new List<DetalleTransferencia>();
        for (var i = 0; i < count; i++)
            detalles.Add(DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!);
        return detalles;
    }

    private Transferencia CreateValid(List<DetalleTransferencia>? detalles = null) =>
        Transferencia.Create(_origenId, _destinoId, _emisorId, detalles ?? ValidDetalles()).Value!;

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsAreValid()
    {
        var result = Transferencia.Create(_origenId, _destinoId, _emisorId, ValidDetalles());

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.EstadoLogistico.Should().Be(EstadoLogistico.Iniciada);
    }

    [Fact]
    public void Create_ShouldFail_WhenSucursalOrigenIsEmpty()
    {
        var result = Transferencia.Create(Guid.Empty, _destinoId, _emisorId, ValidDetalles());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.SucursalOrigenId");
    }

    [Fact]
    public void Create_ShouldFail_WhenSucursalDestinoIsEmpty()
    {
        var result = Transferencia.Create(_origenId, Guid.Empty, _emisorId, ValidDetalles());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.SucursalDestinoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenOrigenAndDestinoAreSame()
    {
        var result = Transferencia.Create(_origenId, _origenId, _emisorId, ValidDetalles());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.SucursalDestinoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenEmpleadoEmisorIsEmpty()
    {
        var result = Transferencia.Create(_origenId, _destinoId, Guid.Empty, ValidDetalles());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EmpleadoEmisorId");
    }

    [Fact]
    public void Create_ShouldFail_WhenDetallesAreEmpty()
    {
        var result = Transferencia.Create(_origenId, _destinoId, _emisorId, []);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.Detalles");
    }

    // ── Aprobar ─────────────────────────────────────────────────────────────

    [Fact]
    public void Aprobar_ShouldSucceed_WhenEstadoIsIniciada()
    {
        var transferencia = CreateValid();

        var result = transferencia.Aprobar();

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.Aprobada);
    }

    [Fact]
    public void Aprobar_ShouldFail_WhenEstadoIsNotIniciada()
    {
        var transferencia = CreateValid();
        _ = transferencia.Aprobar(); // Aprobada

        var result = transferencia.Aprobar();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }

    // ── Despachar ────────────────────────────────────────────────────────────

    [Fact]
    public void Despachar_ShouldSucceed_WhenEstadoIsIniciada()
    {
        var transferencia = CreateValid();

        var result = transferencia.Despachar();

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.En_Transito);
    }

    [Fact]
    public void Despachar_ShouldSucceed_WhenEstadoIsAprobada()
    {
        var transferencia = CreateValid();
        _ = transferencia.Aprobar();

        var result = transferencia.Despachar();

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.En_Transito);
    }

    [Fact]
    public void Despachar_ShouldFail_WhenEstadoIsEnTransito()
    {
        var transferencia = CreateValid();
        _ = transferencia.Despachar();

        var result = transferencia.Despachar();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }

    // ── Recibir ──────────────────────────────────────────────────────────────

    [Fact]
    public void Recibir_ShouldFail_WhenEstadoIsNotEnTransito()
    {
        var transferencia = CreateValid();
        var receptorId = Guid.NewGuid();

        var result = transferencia.Recibir(receptorId, []);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }

    [Fact]
    public void Recibir_ShouldFail_WhenEmpleadoReceptorIsEmpty()
    {
        var transferencia = CreateValid();
        _ = transferencia.Despachar();

        var result = transferencia.Recibir(Guid.Empty, []);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EmpleadoReceptorId");
    }

    [Fact]
    public void Recibir_ShouldFail_WhenLoteDoesNotBelongToTransferencia()
    {
        var transferencia = CreateValid();
        _ = transferencia.Despachar();
        var receptorId = Guid.NewGuid();

        var result = transferencia.Recibir(receptorId, [(Guid.NewGuid(), 5)]);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.Recepcion");
    }

    [Fact]
    public void Recibir_ShouldFail_WhenCantidadRecibidaIsNegative()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();
        var receptorId = Guid.NewGuid();

        var result = transferencia.Recibir(receptorId, [(detalle.LoteId, -1)]);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.Recepcion");
    }

    [Fact]
    public void Recibir_ShouldFail_WhenCantidadRecibidaExceedsCantidadEnviada()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();
        var receptorId = Guid.NewGuid();

        var result = transferencia.Recibir(receptorId, [(detalle.LoteId, 15)]); // 15 > 10

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.Recepcion");
    }

    [Fact]
    public void Recibir_ShouldSetEstadoCompletada_WhenAllQuantitiesMatch()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();

        var result = transferencia.Recibir(Guid.NewGuid(), [(detalle.LoteId, 10)]);

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.Completada);
    }

    [Fact]
    public void Recibir_ShouldSetEstadoRecibidaParcial_WhenQuantityIsLess()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();

        var result = transferencia.Recibir(Guid.NewGuid(), [(detalle.LoteId, 6)]); // 6 < 10

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.Recibida_Parcial);
    }

    // ── Cancelar ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cancelar_ShouldSucceed_WhenEstadoIsIniciada()
    {
        var transferencia = CreateValid();

        var result = transferencia.Cancelar();

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.Cancelada);
    }

    [Fact]
    public void Cancelar_ShouldSucceed_WhenEstadoIsAprobada()
    {
        var transferencia = CreateValid();
        _ = transferencia.Aprobar();

        var result = transferencia.Cancelar();

        _ = result.IsSuccess.Should().BeTrue();
        _ = transferencia.EstadoLogistico.Should().Be(EstadoLogistico.Cancelada);
    }

    [Fact]
    public void Cancelar_ShouldFail_WhenEstadoIsCompletada()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();
        _ = transferencia.Recibir(Guid.NewGuid(), [(detalle.LoteId, 10)]);

        var result = transferencia.Cancelar();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }

    [Fact]
    public void Cancelar_ShouldFail_WhenEstadoIsRecibidaParcial()
    {
        var detalle = DetalleTransferencia.Create(Guid.NewGuid(), 10).Value!;
        var transferencia = CreateValid([detalle]);
        _ = transferencia.Despachar();
        _ = transferencia.Recibir(Guid.NewGuid(), [(detalle.LoteId, 6)]);

        var result = transferencia.Cancelar();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }

    [Fact]
    public void Cancelar_ShouldFail_WhenAlreadyCancelada()
    {
        var transferencia = CreateValid();
        _ = transferencia.Cancelar();

        var result = transferencia.Cancelar();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.EstadoLogistico");
    }
}

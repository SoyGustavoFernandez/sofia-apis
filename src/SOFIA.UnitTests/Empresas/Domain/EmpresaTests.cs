using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empresas.Domain;

public class EmpresaTests
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSucceed_WithOnlyNombre()
    {
        var result = Empresa.Create("Farmacia Salud");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Nombre.Should().Be("Farmacia Salud");
        _ = result.Value.RUC.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSucceed_WithNombreAndRUC()
    {
        var result = Empresa.Create("Farmacia Salud", "12345678901");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.RUC!.Value.Should().Be("12345678901");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenNombreIsEmpty(string? nombre)
    {
        var result = Empresa.Create(nombre!);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Nombre");
    }

    [Theory]
    [InlineData("1234567890")]     // 10 dígitos
    [InlineData("123456789012")]   // 12 dígitos
    [InlineData("1234567890A")]    // no numérico
    [InlineData("")]
    public void Create_ShouldFail_WhenRUCIsInvalid(string ruc)
    {
        var result = Empresa.Create("Farmacia", ruc);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().StartWith("Ruc.");
    }

    [Fact]
    public void Create_ShouldSetTrialActivo()
    {
        var result = Empresa.Create("Farmacia");

        _ = result.Value!.Estado.Should().Be(EstadoEmpresa.TrialActivo);
    }

    [Fact]
    public void Create_ShouldSetFechaVencimiento30DaysFromNow()
    {
        var antes = DateTimeOffset.UtcNow;
        var result = Empresa.Create("Farmacia");
        var despues = DateTimeOffset.UtcNow;

        _ = result.Value!.FechaVencimiento.Should()
            .BeOnOrAfter(antes.AddDays(30))
            .And.BeOnOrBefore(despues.AddDays(30).AddSeconds(1));
    }

    [Fact]
    public void Create_ShouldSetTenantIdToOwnId()
    {
        var empresa = Empresa.Create("Farmacia").Value!;

        _ = empresa.TenantId.Should().Be(empresa.Id);
    }

    // ── EstaVigente ──────────────────────────────────────────────────────────

    [Fact]
    public void EstaVigente_ShouldBeTrue_WhenTrialActivoWithinPeriod()
    {
        var empresa = Empresa.Create("Farmacia").Value!;

        _ = empresa.EstaVigente.Should().BeTrue();
    }

    [Fact]
    public void EstaVigente_ShouldBeTrue_WhenActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));

        _ = empresa.EstaVigente.Should().BeTrue();
    }

    [Fact]
    public void EstaVigente_ShouldBeFalse_WhenSuspendido()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));
        empresa.Suspender();

        _ = empresa.EstaVigente.Should().BeFalse();
    }

    [Fact]
    public void EstaVigente_ShouldBeFalse_WhenCancelado()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        empresa.Cancelar();

        _ = empresa.EstaVigente.Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteEn_ShouldBeFalse_WhenTrialExpired()
    {
        var empresa = Empresa.Create("Farmacia").Value!;

        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento.AddSeconds(-1)).Should().BeTrue();
        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento).Should().BeFalse();
        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void EstaVigenteEn_ShouldBeFalse_WhenSuspendidoOrCanceladoWithinPeriod()
    {
        var suspendida = Empresa.Create("Farmacia").Value!;
        _ = suspendida.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));
        suspendida.Suspender();
        var cancelada = Empresa.Create("Farmacia").Value!;
        cancelada.Cancelar();

        _ = suspendida.EstaVigenteEn(DateTimeOffset.UtcNow).Should().BeFalse();
        _ = cancelada.EstaVigenteEn(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EstaVigente_ShouldBeFalse_WhenSoftDeleted(bool activa)
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        if (activa)
        {
            _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));
        }

        empresa.IsDeleted = true;

        _ = empresa.EstaVigente.Should().BeFalse();
        _ = empresa.EstaVigenteEn(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    // ── ActivarSuscripcion ────────────────────────────────────────────────────

    [Fact]
    public void ActivarSuscripcion_ShouldChangeEstadoToActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));

        _ = empresa.Estado.Should().Be(EstadoEmpresa.Activo);
    }

    [Fact]
    public void ActivarSuscripcion_ShouldSetFechaVencimiento()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var vence = DateTimeOffset.UtcNow.AddDays(365);

        var result = empresa.ActivarSuscripcion(vence);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empresa.FechaVencimiento.Should().Be(vence);
    }

    [Fact]
    public void ActivarSuscripcion_ShouldFail_WhenFechaIsNotInTheFuture()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var fechaAntes = empresa.FechaVencimiento;

        var result = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddMinutes(-1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.FechaVencimiento");
        _ = empresa.Estado.Should().Be(EstadoEmpresa.TrialActivo);
        _ = empresa.FechaVencimiento.Should().Be(fechaAntes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActivarSuscripcion_ShouldReactivate_WhenSuspendidoOrCancelado(bool suspendida)
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        if (suspendida)
        {
            empresa.Suspender();
        }
        else
        {
            empresa.Cancelar();
        }

        var result = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));

        _ = result.IsSuccess.Should().BeTrue();
        _ = empresa.EstaVigente.Should().BeTrue();
    }

    [Fact]
    public void EstaVigenteEn_ShouldBeFalse_WhenActivoExpired()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));

        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento.AddSeconds(-1)).Should().BeTrue();
        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento).Should().BeFalse();
        _ = empresa.EstaVigenteEn(empresa.FechaVencimiento.AddDays(1)).Should().BeFalse();
    }

    // ── ExtenderTrial ────────────────────────────────────────────────────────

    [Fact]
    public void ExtenderTrial_ShouldSetFechaVencimiento_WhenTrialActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var vence = empresa.FechaVencimiento.AddDays(15);

        var result = empresa.ExtenderTrial(vence);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empresa.Estado.Should().Be(EstadoEmpresa.TrialActivo);
        _ = empresa.FechaVencimiento.Should().Be(vence);
    }

    [Fact]
    public void ExtenderTrial_ShouldFail_WhenNotTrialActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        _ = empresa.ActivarSuscripcion(DateTimeOffset.UtcNow.AddDays(30));
        var fechaAntes = empresa.FechaVencimiento;

        var result = empresa.ExtenderTrial(DateTimeOffset.UtcNow.AddDays(60));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.NoEstaEnPrueba");
        _ = empresa.FechaVencimiento.Should().Be(fechaAntes);
    }

    [Fact]
    public void ExtenderTrial_ShouldFail_WhenFechaIsNotInTheFuture()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var fechaAntes = empresa.FechaVencimiento;

        var result = empresa.ExtenderTrial(DateTimeOffset.UtcNow.AddMinutes(-1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.FechaVencimiento");
        _ = empresa.FechaVencimiento.Should().Be(fechaAntes);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldSucceed_WithValidData()
    {
        var empresa = Empresa.Create("Farmacia Antigua").Value!;

        var result = empresa.Update("Farmacia Nueva", "98765432100");

        _ = result.IsSuccess.Should().BeTrue();
        _ = empresa.Nombre.Should().Be("Farmacia Nueva");
        _ = empresa.RUC!.Value.Should().Be("98765432100");
    }

    [Fact]
    public void Update_ShouldSucceed_WithNullRUC()
    {
        var empresa = Empresa.Create("Farmacia", "12345678901").Value!;

        var result = empresa.Update("Farmacia Actualizada", null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empresa.RUC.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldFail_WhenNombreIsEmpty()
    {
        var empresa = Empresa.Create("Farmacia").Value!;

        var result = empresa.Update("", null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Nombre");
    }

    [Fact]
    public void Create_ShouldNotRequirePrescriptionRegistration_ByDefault() =>
        _ = Empresa.Create("Farmacia Salud").Value!.ExigirRegistroReceta.Should().BeFalse();
}

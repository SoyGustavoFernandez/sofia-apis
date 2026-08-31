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
        empresa.ActivarSuscripcion();

        _ = empresa.EstaVigente.Should().BeTrue();
    }

    [Fact]
    public void EstaVigente_ShouldBeFalse_WhenSuspendido()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        empresa.ActivarSuscripcion();
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

    // ── ActivarSuscripcion ────────────────────────────────────────────────────

    [Fact]
    public void ActivarSuscripcion_ShouldChangeEstadoToActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        empresa.ActivarSuscripcion();

        _ = empresa.Estado.Should().Be(EstadoEmpresa.Activo);
    }

    [Fact]
    public void ActivarSuscripcion_ShouldSetFechaVencimiento()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var antes = DateTimeOffset.UtcNow;
        empresa.ActivarSuscripcion(365);

        _ = empresa.FechaVencimiento.Should().BeAfter(antes.AddDays(364));
    }

    // ── ExtenderTrial ────────────────────────────────────────────────────────

    [Fact]
    public void ExtenderTrial_ShouldExtendFechaVencimiento_WhenTrialActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        var fechaOriginal = empresa.FechaVencimiento;

        empresa.ExtenderTrial(15);

        _ = empresa.FechaVencimiento.Should().BeCloseTo(fechaOriginal.AddDays(15), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExtenderTrial_ShouldNotExtend_WhenNotTrialActivo()
    {
        var empresa = Empresa.Create("Farmacia").Value!;
        empresa.ActivarSuscripcion();
        var fechaAntes = empresa.FechaVencimiento;

        empresa.ExtenderTrial(15);

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
}

using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Domain;

public class DigemidInventarioCuarentenaTests
{
    private static readonly Guid SucursalId = Guid.NewGuid();
    private static readonly Guid LoteId = Guid.NewGuid();
    private static readonly Guid EmpleadoId = Guid.NewGuid();

    [Theory]
    [InlineData("Retenido")]
    [InlineData("Liberado")]
    [InlineData("Destruido")]
    [InlineData("Devuelto")]
    public void Create_ShouldSucceed_WhenEstadoResolucionIsKnown(string estado) =>
        _ = DigemidInventarioCuarentena.Create(SucursalId, LoteId, null, 1, "Vencido", estado, EmpleadoId).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData("retenido")]
    [InlineData("Pendiente")]
    public void Create_ShouldFail_WhenEstadoResolucionIsUnknown(string estado)
    {
        var result = DigemidInventarioCuarentena.Create(SucursalId, LoteId, null, 1, "Vencido", estado, EmpleadoId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DigemidInventarioCuarentena.EstadoResolucion");
    }

    [Fact]
    public void Update_ShouldFail_WhenEstadoResolucionIsUnknown()
    {
        var cuarentena = DigemidInventarioCuarentena.Create(SucursalId, LoteId, null, 1, "Vencido", "Retenido", EmpleadoId).Value!;

        var result = cuarentena.Update(SucursalId, LoteId, null, 1, "Vencido", "RETENIDO", EmpleadoId);

        _ = result.IsFailure.Should().BeTrue();
        _ = cuarentena.EstadoResolucion.Should().Be("Retenido");
    }
}

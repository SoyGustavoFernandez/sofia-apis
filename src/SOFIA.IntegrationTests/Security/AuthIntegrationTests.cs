using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

public class AuthIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_ShouldReturnJwt_WhenCredencialesCorrectas()
    {
        // Arrange — seed de empleado + cuenta
        var (_, username, password) = await SeedCuentaAsync();

        var command = new LoginCommand(username, password);

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.AccessToken.Should().NotBeNullOrEmpty(because: "debe devolver un JWT válido");
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIncorrecto()
    {
        // Arrange
        var (_, username, _) = await SeedCuentaAsync();

        var command = new LoginCommand(username, "contraseña_incorrecta");

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameYaExiste()
    {
        // Arrange
        var (_, username, _) = await SeedCuentaAsync();

        // Intentar registrar otra cuenta con el mismo username (empleado diferente)
        var otroEmpleado = await SeedEmpleadoAsync();
        var command = new RegisterAccountCommand(otroEmpleado, username, "OtroPassword123!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(409, because: "username duplicado debe retornar Conflict");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<(Guid empleadoId, string username, string password)> SeedCuentaAsync()
    {
        var empleadoId = await SeedEmpleadoAsync();
        var username = $"user_{Guid.NewGuid():N}"[..20];
        const string password = "TestPassword123!";

        var command = new RegisterAccountCommand(empleadoId, username, password);
        var result = await Sender.Send(command);
        _ = result.IsSuccess.Should().BeTrue(because: "el seeding de cuenta no debe fallar");

        return (empleadoId, username, password);
    }

    private async Task<Guid> SeedEmpleadoAsync()
    {
        var sucursalId = await SeedSucursalAsync();

        var empleado = Domain.Entities.Empleado.Create(
            sucursalBaseId: sucursalId,
            nombres: $"Test{Guid.NewGuid():N}"[..10],
            apellidoPaterno: "ApellidoTest",
            apellidoMaterno: "MatTest").Value!;

        _ = DbContext.Empleados.Add(empleado);
        _ = await DbContext.SaveChangesAsync();
        return empleado.Id;
    }

    private async Task<Guid> SeedSucursalAsync()
    {
        var sucursal = Domain.Entities.Sucursal.Create(
            nombre: $"Suc{Guid.NewGuid():N}"[..20],
            direccionFisica: "Av. Test 123",
            numeroLicencia: $"LIC{Guid.NewGuid():N}"[..10]).Value!;

        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();
        return sucursal.Id;
    }
}

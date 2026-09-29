using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.Logout;
using SOFIA.Application.Security.Commands.RefreshToken;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.Application.Security.Commands.ResetPassword;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

public class AuthIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_ShouldReturnJwt_WhenCredencialesCorrectas()
    {
        // Arrange — login requires a real company with an active subscription
        var (username, _) = await RegistrarEmpresaAnonimaAsync();

        var command = new LoginCommand(username, "TestPassword123!");

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

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_WhenIssuedDuringAnonymousSignUp()
    {
        // Arrange — sign-up runs anonymously, so its refresh token is stored without tenant
        CurrentUser.Empresa = null;
        var usuario = $"logout_{Guid.NewGuid():N}"[..20];
        var registro = await Sender.Send(new RegistrarEmpresaCommand
        {
            NombreEmpresa = $"Farmacia {usuario}",
            Usuario = usuario,
            Password = "TestPassword123!",
        });
        _ = registro.IsSuccess.Should().BeTrue();

        var cuenta = await DbContext.Cuentas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(c => c.NombreUsuario == usuario);

        // Act — logout runs authenticated, inside the company's tenant
        CurrentUser.Empresa = cuenta.TenantId;
        var logout = await Sender.Send(new LogoutCommand(cuenta.Id));

        CurrentUser.Empresa = null;
        var refresh = await Sender.Send(new RefreshTokenCommand(registro.Value!.RefreshToken));

        // Assert
        _ = logout.IsSuccess.Should().BeTrue();
        _ = refresh.IsFailure.Should().BeTrue(because: "a refresh token must stop working after logout");
        _ = refresh.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_WhenAccessTokenExpiredAndOnlyCookieIsPresent()
    {
        // Arrange — an expired bearer leaves the request anonymous, with no tenant and no account id
        var (_, refreshToken) = await RegistrarEmpresaAnonimaAsync();

        // Act
        var logout = await Sender.Send(new LogoutCommand(RefreshToken: refreshToken));
        var refresh = await Sender.Send(new RefreshTokenCommand(refreshToken));

        // Assert
        _ = logout.IsSuccess.Should().BeTrue();
        _ = refresh.IsFailure.Should().BeTrue(because: "the refresh cookie alone must end the session");
        _ = refresh.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ResetPassword_ShouldRevokeRefreshToken_WhenIssuedDuringAnonymousSignUp()
    {
        // Arrange — the whole recovery flow is anonymous, like sign-up
        var (usuario, refreshToken) = await RegistrarEmpresaAnonimaAsync();
        _ = await Sender.Send(new ForgotPasswordCommand(usuario));

        // Only the hash is stored and the raw token is not delivered yet, so plant a known one
        const string recoveryToken = "known-recovery-token";
        var recoveryHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(recoveryToken)));
        _ = await DbContext.Cuentas
            .IgnoreQueryFilters()
            .Where(c => c.NombreUsuario == usuario)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RecoveryToken, recoveryHash));
        DbContext.ChangeTracker.Clear();

        // Act
        var reset = await Sender.Send(new ResetPasswordCommand(usuario, recoveryToken, "NuevaClave123!"));
        var refresh = await Sender.Send(new RefreshTokenCommand(refreshToken));

        // Assert
        _ = reset.IsSuccess.Should().BeTrue();
        _ = refresh.IsFailure.Should().BeTrue(because: "a refresh token must stop working after a password reset");
        _ = refresh.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Refresh_ShouldRevokeCurrentSession_WhenRotatedTokenIsReplayed()
    {
        // Arrange — rotate once, then age the rotated token past the concurrent-refresh grace period
        var (_, tokenRobado) = await RegistrarEmpresaAnonimaAsync();
        var rotacion = await Sender.Send(new RefreshTokenCommand(tokenRobado));
        _ = rotacion.IsSuccess.Should().BeTrue();

        var hashRobado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenRobado)));
        _ = await DbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.TokenHash == hashRobado)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow.AddMinutes(-5)));
        // The handlers share this DbContext, so the tracked token would hide the aged RevokedAt
        DbContext.ChangeTracker.Clear();

        // Act
        var replay = await Sender.Send(new RefreshTokenCommand(tokenRobado));
        var sesionLegitima = await Sender.Send(new RefreshTokenCommand(rotacion.Value!.RefreshToken));

        // Assert
        _ = replay.StatusCode.Should().Be(401);
        _ = sesionLegitima.IsFailure.Should().BeTrue(because: "replaying a rotated token must end every session of the account");
        _ = sesionLegitima.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task SuspendedCompany_ShouldBlockLoginRefreshAndLiveAccessTokens()
    {
        // Arrange — the company is suspended after its session was issued
        var (usuario, refreshToken, accessToken) = await RegistrarEmpresaAnonimaConTokensAsync();
        var cuenta = await DbContext.Cuentas.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.NombreUsuario == usuario);
        var empresa = await DbContext.Empresas.IgnoreQueryFilters().SingleAsync(e => e.Id == cuenta.TenantId);
        empresa.Suspender();
        _ = await DbContext.SaveChangesAsync();

        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var login = await Sender.Send(new LoginCommand(usuario, "TestPassword123!"));
        var refresh = await Sender.Send(new RefreshTokenCommand(refreshToken));
        var request = await client.GetAsync("/api/v1/auth/me");

        // Assert
        _ = login.StatusCode.Should().Be(403);
        _ = login.Error.Code.Should().Be("Auth.EmpresaNoVigente");
        _ = refresh.StatusCode.Should().Be(401);
        _ = request.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "an access token of a suspended company must stop working");
    }

    [Fact]
    public async Task ActiveCompany_ShouldAcceptLiveAccessToken()
    {
        var (_, _, accessToken) = await RegistrarEmpresaAnonimaConTokensAsync();
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = await client.GetAsync("/api/v1/auth/me");

        _ = request.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForcedPasswordChange_ShouldOnlyAllowChangePasswordUntilDone()
    {
        // Arrange — an admin forces the change while the user already holds a live access token
        var (usuario, _, liveAccessToken) = await RegistrarEmpresaAnonimaConTokensAsync();
        _ = await DbContext.Cuentas
            .IgnoreQueryFilters()
            .Where(c => c.NombreUsuario == usuario)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RequiereCambioClave, true));
        DbContext.ChangeTracker.Clear();

        var liveClient = CreateClient(liveAccessToken);
        var liveRequest = await liveClient.GetAsync("/api/v1/laboratorios/plantilla");

        var (flaggedToken, flaggedLogin) = await LoginAsync(usuario, "TestPassword123!");
        var client = CreateClient(flaggedToken);

        // Act
        var blocked = await client.GetAsync("/api/v1/laboratorios/plantilla");
        var profile = await client.GetAsync("/api/v1/auth/me");
        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "TestPassword123!", newPassword = "NuevaClave123!" });
        _ = change.StatusCode.Should().Be(HttpStatusCode.NoContent, await change.Content.ReadAsStringAsync());
        var oldTokenAfterChange = await client.GetAsync("/api/v1/auth/me");

        var (newToken, newLogin) = await LoginAsync(usuario, "NuevaClave123!");
        var unblocked = await CreateClient(newToken).GetAsync("/api/v1/laboratorios/plantilla");

        // Assert
        _ = liveRequest.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: "the database flag also applies to tokens issued before it was set");
        _ = flaggedLogin.GetProperty("requiereCambioClave").GetBoolean().Should().BeTrue();
        _ = blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()))
        {
            _ = body.RootElement.GetProperty("code").GetString().Should().Be("Auth.CambioClaveRequerido");
        }

        _ = profile.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, because: "the profile stays reachable during a forced change");
        _ = profile.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        _ = oldTokenAfterChange.StatusCode.Should().Be(HttpStatusCode.Unauthorized, because: "changing the password ends every session");
        _ = newLogin.GetProperty("requiereCambioClave").GetBoolean().Should().BeFalse();
        _ = unblocked.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private HttpClient CreateClient(string accessToken)
    {
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<(string accessToken, JsonElement body)> LoginAsync(string usuario, string password)
    {
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { nombreUsuario = usuario, password });
        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        return (body.GetProperty("accessToken").GetString()!, body);
    }

    private async Task<(string usuario, string refreshToken, string accessToken)> RegistrarEmpresaAnonimaConTokensAsync()
    {
        CurrentUser.Empresa = null;
        var usuario = $"reuse_{Guid.NewGuid():N}"[..20];
        var registro = await Sender.Send(new RegistrarEmpresaCommand
        {
            NombreEmpresa = $"Farmacia {usuario}",
            Usuario = usuario,
            Password = "TestPassword123!",
        });
        _ = registro.IsSuccess.Should().BeTrue();

        return (usuario, registro.Value!.RefreshToken, registro.Value.AccessToken);
    }

    private async Task<(string usuario, string refreshToken)> RegistrarEmpresaAnonimaAsync()
    {
        var (usuario, refreshToken, _) = await RegistrarEmpresaAnonimaConTokensAsync();
        return (usuario, refreshToken);
    }

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

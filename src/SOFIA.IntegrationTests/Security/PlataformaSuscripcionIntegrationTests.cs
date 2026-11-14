using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;
using SOFIA.Application.Empresas.Queries.GetEmpresaById;
using SOFIA.Application.Empresas.Queries.GetEmpresas;
using SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;
using SOFIA.Application.Plataforma.Queries.GetConfiguracionPago;
using SOFIA.Application.Plataforma.Queries.GetDashboardPlataforma;
using SOFIA.Application.Security;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Authentication;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

// Platform operator features against the real database: cross-company reach, the policy the Admin role cannot bypass and the shared payment row
public class PlataformaSuscripcionIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    [Fact]
    public async Task GetEmpresas_ShouldReturnOnlyTheOwnCompany_UnlessTheCallerIsPlatformOperator()
    {
        // Arrange
        var (propia, _, _) = await RegistrarEmpresaAsync();
        var (ajena, _, _) = await RegistrarEmpresaAsync();
        CurrentUser.Empresa = propia;

        // Act
        var comoCliente = await Sender.Send(new GetEmpresasQuery { PageSize = 100 });
        var ajenaComoCliente = await Sender.Send(new GetEmpresaByIdQuery(ajena));

        CurrentUser.EsAdminPlataforma = true;
        var ajenaComoOperador = await Sender.Send(new GetEmpresaByIdQuery(ajena));
        var comoOperador = await Sender.Send(new GetEmpresasQuery { Nombre = "Farmacia", PageSize = 100 });

        // Assert
        _ = comoCliente.Value!.Items.Select(e => e.Id).Should().Equal(propia);
        _ = ajenaComoCliente.StatusCode.Should().Be(404, because: "a company must not learn that another company exists");
        _ = ajenaComoOperador.IsSuccess.Should().BeTrue();
        _ = ajenaComoOperador.Value!.CantidadSucursales.Should().Be(1, because: "the branch count must also cross the tenant filter");
        _ = comoOperador.Value!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ActivarSuscripcion_ShouldChangeAnotherCompany_OnlyForThePlatformOperator()
    {
        // Arrange
        var (operador, _, _) = await RegistrarEmpresaAsync();
        var (cliente, _, _) = await RegistrarEmpresaAsync();
        var vence = DateTimeOffset.UtcNow.AddDays(30);
        CurrentUser.Empresa = operador;

        // Act
        var comoCliente = await Sender.Send(new ActivarSuscripcionEmpresaCommand { Id = cliente, FechaVencimiento = vence });
        CurrentUser.EsAdminPlataforma = true;
        var comoOperador = await Sender.Send(new ActivarSuscripcionEmpresaCommand { Id = cliente, FechaVencimiento = vence });

        // Assert
        _ = comoCliente.StatusCode.Should().Be(403);
        _ = comoOperador.IsSuccess.Should().BeTrue();
        var guardada = await DbContext.Empresas.IgnoreQueryFilters().AsNoTracking().SingleAsync(e => e.Id == cliente);
        _ = guardada.Estado.Should().Be(EstadoEmpresa.Activo);
        _ = guardada.FechaVencimiento.Should().BeCloseTo(vence, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Login_ShouldBeRejected_WhenThePaidSubscriptionHasExpired()
    {
        // Arrange: a paid subscription whose date already passed, written directly because the domain refuses past dates
        var (empresaId, usuario, _) = await RegistrarEmpresaAsync();
        _ = await DbContext.Database.ExecuteSqlAsync(
            $"UPDATE Empresas SET Estado = {(int)EstadoEmpresa.Activo}, FechaVencimiento = {DateTimeOffset.UtcNow.AddDays(-1)} WHERE Empresa_ID = {empresaId}");

        // Act
        var login = await Sender.Send(new LoginCommand(usuario, RegistroEmpresaBuilder.Password));

        // Assert
        _ = login.StatusCode.Should().Be(403);
        _ = login.Error.Code.Should().Be("Auth.EmpresaNoVigente");
    }

    [Fact]
    public async Task SubscriptionEndpoint_ShouldFollowTheDatabaseFlag_NotTheAdminRole()
    {
        // Arrange: the caller is the Admin of its own company, which passes every permission check
        var (_, usuario, accessToken) = await RegistrarEmpresaAsync();
        var (cliente, _, _) = await RegistrarEmpresaAsync();
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var url = $"/api/v1/empresas/{cliente}/suscripcion/suspender";

        // Act
        var sinMarca = await client.PutAsync(url, content: null);

        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Seguridad_Cuentas SET Es_Admin_Plataforma = 1 WHERE Nombre_Usuario = {usuario}");
        CurrentUser.EsAdminPlataforma = true;
        var conMarca = await client.PutAsync(url, content: null);

        // Assert
        _ = sinMarca.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: "the Admin role bypass must not reach platform operations");
        _ = conMarca.StatusCode.Should().Be(HttpStatusCode.NoContent, because: "the flag is read from the database on every request, so the same token now passes");
        var guardada = await DbContext.Empresas.IgnoreQueryFilters().AsNoTracking().SingleAsync(e => e.Id == cliente);
        _ = guardada.Estado.Should().Be(EstadoEmpresa.Suspendido);
    }

    [Fact]
    public async Task ConfiguracionPago_ShouldBeReadableByAnyCompanyAndAnonymously_AfterTheOperatorSavesIt()
    {
        // Arrange
        var (operador, _, _) = await RegistrarEmpresaAsync();
        var (cliente, _, _) = await RegistrarEmpresaAsync();
        CurrentUser.Empresa = operador;
        CurrentUser.EsAdminPlataforma = true;

        // Act
        var guardado = await Sender.Send(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", Instrucciones = "Plan mensual", QrImagen = _png });
        var sinCambiarQr = await Sender.Send(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51999888777", YapeNumero = "987654321", Instrucciones = "Plan anual" });

        CurrentUser.EsAdminPlataforma = false;
        CurrentUser.Empresa = cliente;
        var comoCliente = await Sender.Send(new GetConfiguracionPagoQuery());
        CurrentUser.Empresa = null;
        var anonimo = await Sender.Send(new GetConfiguracionPagoQuery());

        // Assert
        _ = guardado.IsSuccess.Should().BeTrue();
        _ = sinCambiarQr.Value.Should().Be(guardado.Value, because: "the configuration is a single row");
        _ = comoCliente.Value!.WhatsappNumero.Should().Be("51999888777");
        _ = comoCliente.Value.YapeNumero.Should().Be("987654321");
        _ = comoCliente.Value.Instrucciones.Should().Be("Plan anual");
        _ = comoCliente.Value.QrDataUrl.Should().Be($"data:image/png;base64,{Convert.ToBase64String(_png)}");
        _ = anonimo.Value.Should().Be(comoCliente.Value);
    }

    [Fact]
    public async Task PlatformOperator_ShouldKeepLoginAndLiveAccessToken_WhenItsOwnCompanyIsNotVigente()
    {
        // Arrange: the operator's own company is suspended, which locks out every other account of it
        var (empresaId, usuario, accessToken) = await RegistrarEmpresaAsync();
        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Seguridad_Cuentas SET Es_Admin_Plataforma = 1 WHERE Nombre_Usuario = {usuario}");
        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Empresas SET Estado = {(int)EstadoEmpresa.Suspendido} WHERE Empresa_ID = {empresaId}");
        // Sign-up tracked the account in this context; without this the login would read the stale, unflagged instance
        DbContext.ChangeTracker.Clear();
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var login = await Sender.Send(new LoginCommand(usuario, RegistroEmpresaBuilder.Password));
        var request = await client.GetAsync("/api/v1/auth/me");

        // Assert
        _ = login.IsSuccess.Should().BeTrue(because: "nobody else could reactivate the operator's own company");
        _ = request.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, because: "a suspended company only cuts the tokens of accounts that are not the platform operator");
    }

    [Fact]
    public async Task EmpresasEndpoint_ShouldServeAPlatformOperatorWithoutAnyRole_ButNothingElseDoes()
    {
        // Arrange: an account stripped of every role, as a legacy operator account can be
        var (empresaId, usuario, _) = await RegistrarEmpresaAsync();
        _ = await DbContext.Database.ExecuteSqlAsync(
            $"DELETE cr FROM Seguridad_Cuentas_Roles cr JOIN Seguridad_Cuentas c ON c.Cuenta_ID = cr.Cuenta_ID WHERE c.Nombre_Usuario = {usuario}");
        DbContext.ChangeTracker.Clear();
        var login = await Sender.Send(new LoginCommand(usuario, RegistroEmpresaBuilder.Password));
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Value!.AccessToken);
        CurrentUser.Empresa = empresaId;

        // Act
        var sinMarca = await client.GetAsync("/api/v1/empresas");

        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Seguridad_Cuentas SET Es_Admin_Plataforma = 1 WHERE Nombre_Usuario = {usuario}");
        CurrentUser.EsAdminPlataforma = true;
        var lista = await client.GetAsync("/api/v1/empresas");
        var detalle = await client.GetAsync($"/api/v1/empresas/{empresaId}");
        var editar = await client.PutAsJsonAsync($"/api/v1/empresas/{empresaId}", new { nombre = "Otro nombre", ruc = (string?)null });
        var otroModulo = await client.GetAsync("/api/v1/sucursales");

        // Assert
        _ = sinMarca.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _ = lista.StatusCode.Should().Be(HttpStatusCode.OK, because: "the operator's work must not depend on its role inside its own company");
        _ = detalle.StatusCode.Should().Be(HttpStatusCode.OK);
        _ = editar.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: "the exemption only covers reading companies");
        _ = otroModulo.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: "being the operator grants no pharmacy permission");
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task PermissionHandler_ShouldLetTheOperatorThrough_OnlyWhenTheRequirementAllowsIt(bool permiteOperador, bool esOperador, bool esperado)
    {
        var handler = new PermissionAuthorizationHandler(
            Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Factory.Services.GetRequiredService<IPermissionCache>());
        List<Claim> claims = [new("empresaId", Guid.NewGuid().ToString())];
        if (esOperador)
        {
            claims.Add(new(PlataformaAdminPolicy.ClaimType, "true"));
        }

        var requirement = new PermissionRequirement("Empresas", "Leer", permiteOperador);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity(claims, "test")), null);

        await handler.HandleAsync(context);

        _ = context.HasSucceeded.Should().Be(esperado);
    }

    [Fact]
    public async Task DashboardPlataforma_ShouldClassifyEveryCompanyOnce_AndListOnlyTheOnesAboutToExpire()
    {
        // Arrange: one company about to expire, one paid but already expired, one suspended and one far from expiring
        var (porVencer, _, _) = await RegistrarEmpresaAsync();
        var (vencida, _, _) = await RegistrarEmpresaAsync();
        var (suspendida, _, _) = await RegistrarEmpresaAsync();
        var (lejana, _, _) = await RegistrarEmpresaAsync();
        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Empresas SET FechaVencimiento = {DateTimeOffset.UtcNow.AddDays(3)} WHERE Empresa_ID = {porVencer}");
        _ = await DbContext.Database.ExecuteSqlAsync(
            $"UPDATE Empresas SET Estado = {(int)EstadoEmpresa.Activo}, FechaVencimiento = {DateTimeOffset.UtcNow.AddDays(-2)} WHERE Empresa_ID = {vencida}");
        _ = await DbContext.Database.ExecuteSqlAsync(
            $"UPDATE Empresas SET Estado = {(int)EstadoEmpresa.Suspendido}, FechaVencimiento = {DateTimeOffset.UtcNow.AddDays(2)} WHERE Empresa_ID = {suspendida}");
        CurrentUser.Empresa = lejana;

        // Act
        var comoCliente = await Sender.Send(new GetDashboardPlataformaQuery(null));
        CurrentUser.EsAdminPlataforma = true;
        var resumen = (await Sender.Send(new GetDashboardPlataformaQuery(DateTimeOffset.UtcNow.AddHours(-1)))).Value!;

        // Assert
        _ = comoCliente.StatusCode.Should().Be(403);
        _ = resumen.Vigentes.Should().Be(resumen.EnPrueba + resumen.ConSuscripcion);
        _ = resumen.Total.Should().Be(resumen.Vigentes + resumen.Vencidas + resumen.Suspendidas + resumen.Canceladas, because: "every company falls in exactly one group");
        _ = resumen.PorVencer.Should().BeGreaterThanOrEqualTo(1).And.BeLessThanOrEqualTo(resumen.Vigentes);
        _ = resumen.Vencidas.Should().BeGreaterThanOrEqualTo(1);
        _ = resumen.Suspendidas.Should().BeGreaterThanOrEqualTo(1);
        _ = resumen.RegistradasEsteMes.Should().BeGreaterThanOrEqualTo(4, because: "the four companies were registered after the given start");
        _ = resumen.DiasPorVencer.Should().Be(GetDashboardPlataformaQueryHandler.DiasPorVencer);
        var listadas = resumen.ProximasAVencer.Select(e => e.Id).ToList();
        _ = listadas.Should().NotContain([vencida, suspendida, lejana]);
        _ = resumen.ProximasAVencer.Should().BeInAscendingOrder(e => e.FechaVencimiento);
        if (resumen.PorVencer <= resumen.ProximasAVencer.Count)
        {
            _ = listadas.Should().Contain(porVencer);
        }
    }

    [Fact]
    public async Task GetConfiguracionPagoEndpoint_ShouldAnswerWithoutAToken()
    {
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync("/api/v1/plataforma/pago");

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<(Guid empresaId, string usuario, string accessToken)> RegistrarEmpresaAsync()
    {
        CurrentUser.Empresa = null;
        var usuario = $"plat_{Guid.NewGuid():N}"[..20];
        var registro = await Sender.Send(RegistroEmpresaBuilder.Nuevo(usuario));
        _ = registro.IsSuccess.Should().BeTrue();

        var cuenta = await DbContext.Cuentas.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.NombreUsuario == usuario);
        return (cuenta.TenantId!.Value, usuario, registro.Value!.AccessToken);
    }
}

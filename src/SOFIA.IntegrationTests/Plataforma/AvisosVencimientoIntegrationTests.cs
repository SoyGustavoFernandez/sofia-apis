using System.Net;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Application.Plataforma.Commands.EnviarAvisosVencimiento;
using SOFIA.Domain.Entities;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Plataforma;

public class AvisosVencimientoIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record Settings(string OwnerRecipient) : IPlatformNotificationSettings;

    private readonly CapturingEmailSender _correos = new();

    private Task<Domain.Common.Result<AvisosVencimientoResultDto>> EjecutarAsync(string duenio = "")
    {
        // A scheduled run has no signed-in user
        CurrentUser.Empresa = null;
        return new EnviarAvisosVencimientoCommandHandler(DbContext, _correos, new Settings(duenio))
            .Handle(new EnviarAvisosVencimientoCommand(), CancellationToken.None);
    }

    // Registers a company and moves its expiry to the given number of Peruvian calendar days from today
    private async Task<(Guid Id, string Correo)> EmpresaQueVenceEnAsync(int dias)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var correo = $"admin.{sufijo}@botica.test";
        var ruc = $"20{Random.Shared.NextInt64(100_000_000, 999_999_999)}";
        CurrentUser.Empresa = null;
        var registro = await Sender.Send(new RegistrarEmpresaCommand
        {
            NombreEmpresa = $"Botica {sufijo}",
            RUC = ruc,
            Usuario = $"admin.{sufijo}",
            Password = "Segura123#",
            AdminNombres = "Ana",
            AdminApellidoPaterno = "Quispe",
            AdminEmail = correo,
            DireccionSede = "Av. Prueba 123"
        });
        _ = registro.IsSuccess.Should().BeTrue();

        var empresa = await DbContext.Empresas.IgnoreQueryFilters().SingleAsync(e => e.Nombre == $"Botica {sufijo}");
        var dia = AvisoEnviado.DiaEnPeru(DateTimeOffset.UtcNow).AddDays(dias);
        _ = empresa.ActivarSuscripcion(new DateTimeOffset(dia.ToDateTime(new TimeOnly(23, 59)), TimeSpan.FromHours(-5)));
        _ = await DbContext.SaveChangesAsync();
        return (empresa.Id, correo);
    }

    [Theory]
    [InlineData(7, "en 7 días")]
    [InlineData(1, "mañana")]
    public async Task Avisos_ShouldMailTheCompanyAdminOnceEvenIfTheRunIsRepeated(int dias, string texto)
    {
        var (_, correo) = await EmpresaQueVenceEnAsync(dias);

        _ = await EjecutarAsync();
        _ = await EjecutarAsync();

        var mensaje = _correos.Messages.Where(m => m.To == correo).Should().ContainSingle().Subject;
        _ = mensaje.Subject.Should().Contain(texto);
    }

    [Fact]
    public async Task Avisos_ShouldNotMailACompanyThatExpiresLater()
    {
        var (_, correo) = await EmpresaQueVenceEnAsync(12);

        _ = await EjecutarAsync();

        _ = _correos.Messages.Should().NotContain(m => m.To == correo);
    }

    [Fact]
    public async Task Resumen_ShouldReachTheOwnerOncePerDayWithTheExpiringCompanies()
    {
        var (_, correo) = await EmpresaQueVenceEnAsync(3);

        var primera = await EjecutarAsync("duenio@sofia.test");
        var segunda = await EjecutarAsync("duenio@sofia.test");

        _ = primera.Value!.ResumenEnviado.Should().BeTrue();
        _ = segunda.Value!.ResumenEnviado.Should().BeFalse();
        var resumen = _correos.Messages.Where(m => m.To == "duenio@sofia.test").Should().ContainSingle().Subject;
        _ = resumen.TextBody.Should().Contain(correo);
    }

    [Theory]
    [InlineData(null, "clave-de-prueba", HttpStatusCode.Unauthorized)]
    [InlineData("otra-clave", "clave-de-prueba", HttpStatusCode.Unauthorized)]
    [InlineData("clave-de-prueba", "", HttpStatusCode.Unauthorized)]
    [InlineData("clave-de-prueba", "clave-de-prueba", HttpStatusCode.OK)]
    public async Task Endpoint_ShouldOnlyRunWithTheConfiguredKey(string? enviada, string configurada, HttpStatusCode esperado)
    {
        CurrentUser.Empresa = null;
        using var factory = Factory.WithWebHostBuilder(b => b.UseSetting("Notifications:TasksKey", configurada));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tareas/avisos-vencimiento");
        if (enviada is not null)
        {
            request.Headers.Add("X-Tareas-Key", enviada);
        }

        using var response = await client.SendAsync(request);

        _ = response.StatusCode.Should().Be(esperado);
    }
}

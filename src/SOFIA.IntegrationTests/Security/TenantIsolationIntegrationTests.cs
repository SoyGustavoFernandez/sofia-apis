using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Application.Empleados.Commands.UpdateEmpleado;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Application.Security.Commands.RefreshToken;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Authentication;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Security;

public class TenantIsolationIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly Guid EmpresaA = Guid.NewGuid();
    private static readonly Guid EmpresaB = Guid.NewGuid();

    [Fact]
    public async Task SaveChanges_ShouldAssignCurrentTenant_WhenEntityIsAdded()
    {
        // Arrange
        CurrentUser.Empresa = EmpresaA;
        var laboratorio = Laboratorio.Create($"Lab{Guid.NewGuid():N}"[..20], null).Value!;

        // Act
        _ = DbContext.Laboratorios.Add(laboratorio);
        _ = await DbContext.SaveChangesAsync();

        // Assert
        _ = laboratorio.TenantId.Should().Be(EmpresaA);
    }

    [Fact]
    public async Task Query_ShouldHideRows_WhenTheyBelongToAnotherTenant()
    {
        // Arrange
        CurrentUser.Empresa = EmpresaA;
        var laboratorio = Laboratorio.Create($"Lab{Guid.NewGuid():N}"[..20], null).Value!;
        _ = DbContext.Laboratorios.Add(laboratorio);
        _ = await DbContext.SaveChangesAsync();

        // Act
        CurrentUser.Empresa = EmpresaB;
        var visibleFromB = await DbContext.Laboratorios.AsNoTracking().AnyAsync(l => l.Id == laboratorio.Id);

        CurrentUser.Empresa = EmpresaA;
        var visibleFromA = await DbContext.Laboratorios.AsNoTracking().AnyAsync(l => l.Id == laboratorio.Id);

        // Assert
        _ = visibleFromB.Should().BeFalse(because: "a company must never read another company's data");
        _ = visibleFromA.Should().BeTrue();
    }

    [Fact]
    public async Task Query_ShouldHideRowsWithoutTenant_ForAnyCompanyAndForAnonymousContext()
    {
        // Arrange: row written with no tenant in context keeps TenantId = NULL (legacy data)
        CurrentUser.Empresa = null;
        var laboratorio = Laboratorio.Create($"Lab{Guid.NewGuid():N}"[..20], null).Value!;
        _ = DbContext.Laboratorios.Add(laboratorio);
        _ = await DbContext.SaveChangesAsync();

        // Act
        var visibleAnonymous = await DbContext.Laboratorios.AsNoTracking().AnyAsync(l => l.Id == laboratorio.Id);

        CurrentUser.Empresa = EmpresaA;
        var visibleFromA = await DbContext.Laboratorios.AsNoTracking().AnyAsync(l => l.Id == laboratorio.Id);

        // Assert
        _ = visibleAnonymous.Should().BeFalse(because: "no tenant in context must not mean every tenant");
        _ = visibleFromA.Should().BeFalse(because: "NULL-tenant rows are not shared across companies");
    }

    [Fact]
    public async Task UniqueIndex_ShouldAllowSameDocument_WhenPatientsBelongToDifferentTenants()
    {
        // Arrange
        var documento = $"{Random.Shared.Next(10_000_000, 99_999_999)}";

        CurrentUser.Empresa = EmpresaA;
        _ = DbContext.Pacientes.Add(PacienteCliente.Create(documento, "Paciente A", new DateOnly(1990, 1, 1), null).Value!);
        _ = await DbContext.SaveChangesAsync();

        // Act
        CurrentUser.Empresa = EmpresaB;
        _ = DbContext.Pacientes.Add(PacienteCliente.Create(documento, "Paciente B", new DateOnly(1990, 1, 1), null).Value!);
        var act = () => DbContext.SaveChangesAsync();

        // Assert
        _ = await act.Should().NotThrowAsync(because: "the same person can be a patient of two different pharmacies");
    }

    [Fact]
    public async Task RegistrarEmpresa_ShouldCreateOwnAdminRole_WhenTwoCompaniesSignUp()
    {
        // Arrange: public sign-up runs without any tenant in context
        CurrentUser.Empresa = null;
        var comandoA = NewRegistro();
        var comandoB = NewRegistro();

        // Act
        var resultA = await Sender.Send(comandoA);
        var resultB = await Sender.Send(comandoB);

        // Assert
        _ = resultA.IsSuccess.Should().BeTrue();
        _ = resultB.IsSuccess.Should().BeTrue(because: "each company gets its own 'Admin' role, so the name does not collide");

        var admins = await DbContext.Roles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.Cuentas.Any(c => c.NombreUsuario == comandoA.Usuario || c.NombreUsuario == comandoB.Usuario))
            .ToListAsync();

        _ = admins.Should().HaveCount(2);
        _ = admins.Should().OnlyContain(r => r.NombreRol == "Admin" && r.TenantId != null);
        _ = admins.Select(r => r.TenantId).Distinct().Should().HaveCount(2, because: "Admin privileges must not be shared between companies");
    }

    [Fact]
    public async Task Login_ShouldSucceed_WhenAccountBelongsToATenantAndRequestIsAnonymous()
    {
        // Arrange
        CurrentUser.Empresa = null;
        var registro = NewRegistro();
        _ = (await Sender.Send(registro)).IsSuccess.Should().BeTrue();

        // Act
        var result = await Sender.Send(new LoginCommand(registro.Usuario, registro.Password));

        // Assert
        _ = result.IsSuccess.Should().BeTrue(because: "login resolves the tenant from the account, not from the request");
    }

    [Fact]
    public async Task LoginAndRefresh_ShouldIgnoreLinkedRoles_WhenTheyBelongToAnotherTenantOrToNone()
    {
        // Arrange: a stray link from company A's account to a role of company B and to a NULL-tenant role
        CurrentUser.Empresa = null;
        var registro = NewRegistro();
        var registroResult = await Sender.Send(registro);
        _ = registroResult.IsSuccess.Should().BeTrue();

        var rolSinTenant = Rol.Create($"Huerfano{Guid.NewGuid():N}"[..20], null).Value!;
        _ = DbContext.Roles.Add(rolSinTenant);
        _ = await DbContext.SaveChangesAsync();

        CurrentUser.Empresa = EmpresaB;
        var rolAjeno = Rol.Create($"Ajeno{Guid.NewGuid():N}"[..20], null).Value!;
        _ = DbContext.Roles.Add(rolAjeno);
        _ = await DbContext.SaveChangesAsync();

        var cuenta = await DbContext.Cuentas.IgnoreQueryFilters().Include(c => c.Roles).SingleAsync(c => c.NombreUsuario == registro.Usuario);
        cuenta.AddRol(rolAjeno);
        cuenta.AddRol(rolSinTenant);
        _ = await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        CurrentUser.Empresa = null;

        // Act
        var login = await Sender.Send(new LoginCommand(registro.Usuario, registro.Password));
        var refresh = await Sender.Send(new RefreshTokenCommand(login.Value!.RefreshToken));

        // Assert
        foreach (var accessToken in new[] { login.Value!.AccessToken, refresh.Value!.AccessToken })
        {
            var valores = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims.Select(c => c.Value).ToList();
            _ = valores.Should().Contain("Admin");
            _ = valores.Should().NotContain([rolAjeno.NombreRol, rolSinTenant.NombreRol], because: "only the account's own company roles may reach the token");
        }
    }

    [Fact]
    public async Task PermissionHandler_ShouldNotShareCachedPermissions_WhenRoleNameRepeatsAcrossTenants()
    {
        // Arrange: both companies have a "Cajero" role, only company A grants Ventas:Crear
        var rolName = $"Cajero{Guid.NewGuid():N}"[..20];

        CurrentUser.Empresa = EmpresaA;
        var rolA = Rol.Create(rolName, null).Value!;
        _ = DbContext.Roles.Add(rolA);
        _ = DbContext.PermisosRol.Add(PermisoRol.Create(rolA.Id, "Ventas", "Crear").Value!);
        _ = await DbContext.SaveChangesAsync();

        CurrentUser.Empresa = EmpresaB;
        _ = DbContext.Roles.Add(Rol.Create(rolName, null).Value!);
        _ = await DbContext.SaveChangesAsync();

        var handler = new PermissionAuthorizationHandler(
            Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Factory.Services.GetRequiredService<IPermissionCache>());

        // Act: company A warms the cache first, then company B asks with the same role name
        CurrentUser.Empresa = EmpresaA;
        var grantedA = await AuthorizeAsync(handler, EmpresaA, rolName);

        CurrentUser.Empresa = EmpresaB;
        var grantedB = await AuthorizeAsync(handler, EmpresaB, rolName);

        // Assert
        _ = grantedA.Should().BeTrue();
        _ = grantedB.Should().BeFalse(because: "a role in company B must not inherit permissions cached for company A");
    }

    [Fact]
    public async Task UpdateEmpleado_ShouldRejectBaseBranch_WhenItBelongsToAnotherTenant()
    {
        // Arrange: the branch lives in company B, the employee in company A
        CurrentUser.Empresa = EmpresaB;
        var sucursalB = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. B 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursalB);
        _ = await DbContext.SaveChangesAsync();

        CurrentUser.Empresa = EmpresaA;
        var sucursalA = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. A 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursalA);
        _ = await DbContext.SaveChangesAsync();
        var empleado = Empleado.Create(sucursalA.Id, "Ana", "Perez", "Gomez").Value!;
        _ = DbContext.Empleados.Add(empleado);
        _ = await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new UpdateEmpleadoCommand
        {
            Id = empleado.Id,
            Sucursal_Base_ID = sucursalB.Id,
            Nombres = "Ana",
            Apellido_Paterno = "Perez",
            Apellido_Materno = "Gomez",
        });

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NotFound", because: "the FK has no tenant column, so the handler must reject foreign branches");
        _ = empleado.Sucursal_Base_ID.Should().Be(sucursalA.Id);
    }

    private static async Task<bool> AuthorizeAsync(PermissionAuthorizationHandler handler, Guid empresaId, string rolName)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, rolName), new Claim("empresaId", empresaId.ToString())], "test"));
        var requirement = new PermissionRequirement("Ventas", "Crear");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static RegistrarEmpresaCommand NewRegistro() => new()
    {
        NombreEmpresa = $"Farmacia {Guid.NewGuid():N}"[..30],
        Usuario = $"u{Guid.NewGuid():N}"[..20],
        Password = "TestPassword123!"
    };
}

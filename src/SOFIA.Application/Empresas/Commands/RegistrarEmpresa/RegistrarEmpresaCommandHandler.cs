using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.ValueObjects;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

public class RegistrarEmpresaCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider) : IRequestHandler<RegistrarEmpresaCommand, Result<LoginResult>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<LoginResult>> Handle(RegistrarEmpresaCommand request, CancellationToken cancellationToken)
    {
        if (request.RUC is not null)
        {
            var rucVo = Ruc.Create(request.RUC).Value!;
            // Anonymous sign-up: RUC and username uniqueness are global, not per tenant
            var rucTomado = await context.Empresas
                .IgnoreQueryFilters([QueryFilters.Tenant])
                .AnyAsync(e => e.RUC == rucVo && !e.IsDeleted, cancellationToken);
            if (rucTomado)
            {
                return Result.Failure<LoginResult>(Error.Conflict("Empresa.RUC.Duplicado", "Ya existe una empresa registrada con este RUC."), 409);
            }
        }

        var usuarioTomado = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AnyAsync(c => c.NombreUsuario == request.Usuario && !c.IsDeleted, cancellationToken);
        if (usuarioTomado)
        {
            return Result.Failure<LoginResult>(Error.Conflict("Auth.DuplicateUsername", "Username is already in use."), 409);
        }

        var entityResult = CreateEntityChain(request, passwordHasher);
        if (entityResult.IsFailure)
        {
            return Result.Failure<LoginResult>(entityResult.Error);
        }

        var (empresa, sucursal, empleado, cuenta, rolAdmin) = entityResult.Value;

        cuenta.AddRol(rolAdmin);

        _ = context.Roles.Add(rolAdmin);
        _ = context.Empresas.Add(empresa);
        _ = context.Sucursales.Add(sucursal);
        _ = context.Empleados.Add(empleado);
        _ = context.Cuentas.Add(cuenta);

        // Same session pair as login, so the new admin can silently refresh instead of re-logging in
        var (rawToken, tokenHash) = TokenHasher.GenerateRefreshToken();
        var expiry = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);
        _ = context.RefreshTokens.Add(DomainRefreshToken.Create(cuenta.Id, tokenHash, expiry));

        _ = await context.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.Generate(cuenta, empresa.Id, sucursal.Id);
        return Result.Success(new LoginResult(accessToken, rawToken, expiry), 201);
    }

    private static Result<(Empresa, Sucursal, Empleado, Cuenta, Rol)> CreateEntityChain(RegistrarEmpresaCommand request, IPasswordHasher passwordHasher)
    {
        var empresaResult = Empresa.Create(request.NombreEmpresa, request.RUC);
        if (empresaResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta, Rol)>(empresaResult.Error);
        }

        var empresa = empresaResult.Value!;

        var nombreSede = string.IsNullOrWhiteSpace(request.NombreSede) ? "Sede Principal" : request.NombreSede;
        var direccionSede = string.IsNullOrWhiteSpace(request.DireccionSede) ? "Por definir" : request.DireccionSede;
        var numeroLicencia = string.IsNullOrWhiteSpace(request.NumeroLicencia) ? "Por definir" : request.NumeroLicencia;

        var sucursalResult = Sucursal.Create(nombreSede, direccionSede, numeroLicencia, empresaId: empresa.Id);
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta, Rol)>(sucursalResult.Error);
        }

        var sucursal = sucursalResult.Value!;

        var adminNombres = string.IsNullOrWhiteSpace(request.AdminNombres) ? request.Usuario : request.AdminNombres;
        var adminApPat = string.IsNullOrWhiteSpace(request.AdminApellidoPaterno) ? "-" : request.AdminApellidoPaterno;
        var adminApMat = string.IsNullOrWhiteSpace(request.AdminApellidoMaterno) ? "-" : request.AdminApellidoMaterno;

        var empleadoResult = Empleado.Create(sucursal.Id, adminNombres, adminApPat, adminApMat, tenantId: empresa.Id);
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta, Rol)>(empleadoResult.Error);
        }

        var empleado = empleadoResult.Value!;

        var passwordHash = passwordHasher.Hash(request.Password);
        var cuentaResult = Cuenta.Create(empleado.Id, request.Usuario, passwordHash, tenantId: empresa.Id, requiereCambioClave: false);
        if (cuentaResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta, Rol)>(cuentaResult.Error);
        }

        // Each company gets its own Admin role so its privileges never reach other tenants
        var rolResult = Rol.Create(Rol.AdminRoleName, "Administrador de la empresa", tenantId: empresa.Id);
        return rolResult.IsFailure
            ? Result.Failure<(Empresa, Sucursal, Empleado, Cuenta, Rol)>(rolResult.Error)
            : Result.Success<(Empresa, Sucursal, Empleado, Cuenta, Rol)>((empresa, sucursal, empleado, cuentaResult.Value!, rolResult.Value!));
    }
}

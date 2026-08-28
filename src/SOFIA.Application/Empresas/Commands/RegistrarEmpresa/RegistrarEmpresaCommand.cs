using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

public record RegistrarEmpresaCommand : ICommand<string>
{
    // Mínimo obligatorio
    public string NombreEmpresa { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    // Opcionales — completables desde el perfil de la empresa después del registro
    public string? RUC { get; init; }
    public string? NombreSede { get; init; }
    public string? DireccionSede { get; init; }
    public string? NumeroLicencia { get; init; }
    public string? AdminNombres { get; init; }
    public string? AdminApellidoPaterno { get; init; }
    public string? AdminApellidoMaterno { get; init; }
}

public class RegistrarEmpresaCommandValidator : AbstractValidator<RegistrarEmpresaCommand>
{
    public RegistrarEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.NombreEmpresa)
            .NotEmpty().WithMessage("El nombre de la empresa es requerido.")
            .MaximumLength(200);

        _ = RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El usuario es requerido.")
            .MinimumLength(4).WithMessage("El usuario debe tener al menos 4 caracteres.")
            .MaximumLength(50);

        _ = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        _ = When(x => x.RUC is not null, () =>
            RuleFor(x => x.RUC)
                .Length(11).WithMessage("El RUC debe tener exactamente 11 dígitos.")
                .Matches("^[0-9]{11}$").WithMessage("El RUC debe contener solo dígitos."));
    }
}

public class RegistrarEmpresaCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider) : IRequestHandler<RegistrarEmpresaCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RegistrarEmpresaCommand request, CancellationToken cancellationToken)
    {
        if (request.RUC is not null)
        {
            var rucTomado = await context.Empresas
                .AnyAsync(e => e.RUC == request.RUC && !e.IsDeleted, cancellationToken);
            if (rucTomado)
            {
                return Result.Failure<string>(Error.Conflict("Empresa.RUC.Duplicado", "Ya existe una empresa registrada con este RUC."), 409);
            }
        }

        var usuarioTomado = await context.Cuentas
            .AnyAsync(c => c.NombreUsuario == request.Usuario && !c.IsDeleted, cancellationToken);
        if (usuarioTomado)
        {
            return Result.Failure<string>(Error.Conflict("Auth.DuplicateUsername", "El nombre de usuario ya está en uso."), 409);
        }

        var empresaResult = Empresa.Create(request.NombreEmpresa, request.RUC);
        if (empresaResult.IsFailure)
        {
            return Result.Failure<string>(empresaResult.Error);
        }

        var empresa = empresaResult.Value!;

        var nombreSede = string.IsNullOrWhiteSpace(request.NombreSede) ? "Sede Principal" : request.NombreSede;
        var direccionSede = string.IsNullOrWhiteSpace(request.DireccionSede) ? "Por definir" : request.DireccionSede;
        var numeroLicencia = string.IsNullOrWhiteSpace(request.NumeroLicencia) ? "Por definir" : request.NumeroLicencia;

        var sucursalResult = Sucursal.Create(nombreSede, direccionSede, numeroLicencia, empresaId: empresa.Id);
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<string>(sucursalResult.Error);
        }

        var sucursal = sucursalResult.Value!;

        var adminNombres = string.IsNullOrWhiteSpace(request.AdminNombres) ? "Administrador" : request.AdminNombres;
        var adminApPat = string.IsNullOrWhiteSpace(request.AdminApellidoPaterno) ? request.NombreEmpresa[..Math.Min(request.NombreEmpresa.Length, 50)] : request.AdminApellidoPaterno;
        var adminApMat = string.IsNullOrWhiteSpace(request.AdminApellidoMaterno) ? "Sistema" : request.AdminApellidoMaterno;

        var empleadoResult = Empleado.Create(sucursal.Id, adminNombres, adminApPat, adminApMat, tenantId: empresa.Id);
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<string>(empleadoResult.Error);
        }

        var empleado = empleadoResult.Value!;

        var passwordHash = passwordHasher.Hash(request.Password);
        var cuentaResult = Cuenta.Create(empleado.Id, request.Usuario, passwordHash, tenantId: empresa.Id, requiereCambioClave: false);
        if (cuentaResult.IsFailure)
        {
            return Result.Failure<string>(cuentaResult.Error);
        }

        var cuenta = cuentaResult.Value!;

        var rolAdmin = await context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "Admin", cancellationToken);
        if (rolAdmin is not null)
        {
            cuenta.AddRol(rolAdmin);
        }

        _ = context.Empresas.Add(empresa);
        _ = context.Sucursales.Add(sucursal);
        _ = context.Empleados.Add(empleado);
        _ = context.Cuentas.Add(cuenta);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Pasamos empresaId y sucursalId directamente — no necesitamos recargar navegaciones
        return Result.Success(jwtProvider.Generate(cuenta, empresa.Id, sucursal.Id), 201);
    }
}

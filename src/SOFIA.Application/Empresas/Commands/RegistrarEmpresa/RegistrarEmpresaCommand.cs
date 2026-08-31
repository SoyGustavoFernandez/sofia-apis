using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

public record RegistrarEmpresaCommand : ICommand<string>
{
    // Minimum required
    public string NombreEmpresa { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    // Optional — completable from the company profile after registration
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
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        _ = RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(4).WithMessage("Username must be at least 4 characters long.")
            .MaximumLength(50);

        _ = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        _ = When(x => x.RUC is not null, () =>
            RuleFor(x => x.RUC)
                .Length(11).WithMessage("RUC must be exactly 11 digits.")
                .Matches("^[0-9]{11}$").WithMessage("RUC must contain only digits."));
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
            var rucVo = Ruc.Create(request.RUC).Value!;
            var rucTomado = await context.Empresas
                .AnyAsync(e => e.RUC == rucVo && !e.IsDeleted, cancellationToken);
            if (rucTomado)
            {
                return Result.Failure<string>(Error.Conflict("Empresa.RUC.Duplicado", "Ya existe una empresa registrada con este RUC."), 409);
            }
        }

        var usuarioTomado = await context.Cuentas
            .AnyAsync(c => c.NombreUsuario == request.Usuario && !c.IsDeleted, cancellationToken);
        if (usuarioTomado)
        {
            return Result.Failure<string>(Error.Conflict("Auth.DuplicateUsername", "Username is already in use."), 409);
        }

        var entityResult = CreateEntityChain(request, passwordHasher);
        if (entityResult.IsFailure)
        {
            return Result.Failure<string>(entityResult.Error);
        }

        var (empresa, sucursal, empleado, cuenta) = entityResult.Value;

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

        return Result.Success(jwtProvider.Generate(cuenta, empresa.Id, sucursal.Id), 201);
    }

    private static Result<(Empresa, Sucursal, Empleado, Cuenta)> CreateEntityChain(RegistrarEmpresaCommand request, IPasswordHasher passwordHasher)
    {
        var empresaResult = Empresa.Create(request.NombreEmpresa, request.RUC);
        if (empresaResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta)>(empresaResult.Error);
        }

        var empresa = empresaResult.Value!;

        var nombreSede = string.IsNullOrWhiteSpace(request.NombreSede) ? "Sede Principal" : request.NombreSede;
        var direccionSede = string.IsNullOrWhiteSpace(request.DireccionSede) ? "Por definir" : request.DireccionSede;
        var numeroLicencia = string.IsNullOrWhiteSpace(request.NumeroLicencia) ? "Por definir" : request.NumeroLicencia;

        var sucursalResult = Sucursal.Create(nombreSede, direccionSede, numeroLicencia, empresaId: empresa.Id);
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta)>(sucursalResult.Error);
        }

        var sucursal = sucursalResult.Value!;

        var adminNombres = string.IsNullOrWhiteSpace(request.AdminNombres) ? request.Usuario : request.AdminNombres;
        var adminApPat = string.IsNullOrWhiteSpace(request.AdminApellidoPaterno) ? "-" : request.AdminApellidoPaterno;
        var adminApMat = string.IsNullOrWhiteSpace(request.AdminApellidoMaterno) ? "-" : request.AdminApellidoMaterno;

        var empleadoResult = Empleado.Create(sucursal.Id, adminNombres, adminApPat, adminApMat, tenantId: empresa.Id);
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<(Empresa, Sucursal, Empleado, Cuenta)>(empleadoResult.Error);
        }

        var empleado = empleadoResult.Value!;

        var passwordHash = passwordHasher.Hash(request.Password);
        var cuentaResult = Cuenta.Create(empleado.Id, request.Usuario, passwordHash, tenantId: empresa.Id, requiereCambioClave: false);
        return cuentaResult.IsFailure
            ? Result.Failure<(Empresa, Sucursal, Empleado, Cuenta)>(cuentaResult.Error)
            : Result.Success<(Empresa, Sucursal, Empleado, Cuenta)>((empresa, sucursal, empleado, cuentaResult.Value!));
    }
}

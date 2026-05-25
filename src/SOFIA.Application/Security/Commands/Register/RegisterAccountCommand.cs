using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Register;

public record RegisterAccountCommand(
    Guid EmpleadoId,
    string NombreUsuario,
    string Password) : IRequest<Result<Guid>>;

public class RegisterAccountCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        // Verificar si el empleado existe
        var empleadoExists = await context.Empleados
            .AnyAsync(e => e.Id == request.EmpleadoId && !e.IsDeleted, cancellationToken);

        if (!empleadoExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."));
        }

        // Verificar si ya tiene una cuenta (Relación 1:1)
        var cuentaExists = await context.Cuentas
            .AnyAsync(c => c.EmpleadoId == request.EmpleadoId && !c.IsDeleted, cancellationToken);

        if (cuentaExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateAccount", "Este empleado ya tiene una cuenta de usuario vinculada."));
        }

        // Verificar si el nombre de usuario ya está tomado
        var usernameTaken = await context.Cuentas
            .AnyAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (usernameTaken)
        {
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateUsername", "El nombre de usuario ya está en uso."));
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var result = Cuenta.Create(request.EmpleadoId, request.NombreUsuario, passwordHash);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Cuentas.Add(result.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value!.Id);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Register;

public record RegisterAccountCommand(
    Guid EmpleadoId,
    string NombreUsuario,
    string Password) : ICommand<Guid>;

public class RegisterAccountCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        // These 3 checks must be sequential — EF Core does not support concurrent queries on the same DbContext
        var empleadoExists = await context.Empleados
            .AnyAsync(e => e.Id == request.EmpleadoId && !e.IsDeleted, cancellationToken);
        var cuentaExists = await context.Cuentas
            .AnyAsync(c => c.EmpleadoId == request.EmpleadoId && !c.IsDeleted, cancellationToken);
        var usernameTaken = await context.Cuentas
            .AnyAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        if (!empleadoExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."), 404);
        }

        if (cuentaExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateAccount", "Este empleado ya tiene una cuenta de usuario vinculada."), 409);
        }

        if (usernameTaken)
        {
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateUsername", "El nombre de usuario ya está en uso."), 409);
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

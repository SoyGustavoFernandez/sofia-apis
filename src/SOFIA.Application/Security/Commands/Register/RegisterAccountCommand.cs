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
        // Ejecutar las 3 verificaciones en paralelo
        var empleadoExistsTask = context.Empleados
            .AnyAsync(e => e.Id == request.EmpleadoId && !e.IsDeleted, cancellationToken);
        var cuentaExistsTask = context.Cuentas
            .AnyAsync(c => c.EmpleadoId == request.EmpleadoId && !c.IsDeleted, cancellationToken);
        var usernameTakenTask = context.Cuentas
            .AnyAsync(c => c.NombreUsuario == request.NombreUsuario && !c.IsDeleted, cancellationToken);

        _ = await Task.WhenAll(empleadoExistsTask, cuentaExistsTask, usernameTakenTask);

        if (!empleadoExistsTask.Result)
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."));

        if (cuentaExistsTask.Result)
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateAccount", "Este empleado ya tiene una cuenta de usuario vinculada."));

        if (usernameTakenTask.Result)
            return Result.Failure<Guid>(Error.Conflict("Auth.DuplicateUsername", "El nombre de usuario ya está en uso."));

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

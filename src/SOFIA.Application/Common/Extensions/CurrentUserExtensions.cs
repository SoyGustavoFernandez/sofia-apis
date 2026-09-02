using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Common.Extensions;

public static class CurrentUserExtensions
{
    public static Result<Guid> GetSucursalId(this ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure<Guid>(Error.Unauthorized("Auth.Sucursal", "User must be authenticated and assigned to a branch."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalId))
        {
            return Result.Failure<Guid>(Error.Validation("Auth.Sucursal", "Invalid branch ID in user context."));
        }

        return Result.Success(sucursalId);
    }

    public static Result<Guid> GetEmpleadoId(this ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure<Guid>(Error.Unauthorized("Auth.Empleado", "User must be authenticated."));
        }

        if (!Guid.TryParse(currentUser.Id, out var empleadoId))
        {
            return Result.Failure<Guid>(Error.Validation("Auth.Empleado", "Invalid employee ID in user context."));
        }

        return Result.Success(empleadoId);
    }

    public static Result<Guid> GetEmpresaId(this ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.EmpresaId))
        {
            return Result.Failure<Guid>(Error.Unauthorized("Auth.Empresa", "User must be authenticated and assigned to a company."));
        }

        if (!Guid.TryParse(currentUser.EmpresaId, out var empresaId))
        {
            return Result.Failure<Guid>(Error.Validation("Auth.Empresa", "Invalid company ID in user context."));
        }

        return Result.Success(empresaId);
    }
}

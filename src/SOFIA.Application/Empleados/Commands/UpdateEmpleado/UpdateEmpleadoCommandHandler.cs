using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<UpdateEmpleadoCommand, Result>
{
    public async Task<Result> Handle(UpdateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var empleado = await context.Empleados
            .FindAsync([request.Id], cancellationToken);

        if (empleado is null)
        {
            return Result.Failure(Error.NotFound("Empleado.NotFound", $"Empleado with ID {request.Id} was not found."), 404);
        }

        var sucursalCambia = empleado.Sucursal_Base_ID != request.Sucursal_Base_ID;

        if (sucursalCambia)
        {
            // The base branch drives the session branch, so nobody can move themselves
            if (empleado.Id.ToString() == currentUser.Id)
            {
                return Result.Failure(Error.Forbidden("Empleado.SucursalBase.AutoAsignacion", "No puedes cambiar tu propia sucursal base."), 403);
            }

            // Tenant-filtered lookup: a branch of another company is reported as missing
            var sucursalExists = await context.Sucursales.AnyAsync(s => s.Id == request.Sucursal_Base_ID, cancellationToken);
            if (!sucursalExists)
            {
                return Result.Failure(Error.NotFound("Sucursal.NotFound", "La sucursal no existe."), 404);
            }
        }

        var result = empleado.Update(
            request.Sucursal_Base_ID,
            request.Nombres,
            request.Apellido_Paterno,
            request.Apellido_Materno,
            request.Licencia_Prof,
            request.Huella_Biometrica);

        if (!result.IsSuccess)
        {
            return result;
        }

        if (sucursalCambia)
        {
            // Live access tokens still carry the old branch, so they must stop working
            var cuentas = await context.Cuentas
                .Where(c => c.EmpleadoId == empleado.Id)
                .ToListAsync(cancellationToken);

            foreach (var cuenta in cuentas)
            {
                cuenta.InvalidateSecurityStamp();
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

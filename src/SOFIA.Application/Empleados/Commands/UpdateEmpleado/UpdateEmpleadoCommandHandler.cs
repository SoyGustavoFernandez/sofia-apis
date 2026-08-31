using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateEmpleadoCommand, Result>
{
    public async Task<Result> Handle(UpdateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var empleado = await context.Empleados
            .FindAsync([request.Id], cancellationToken);

        if (empleado is null)
        {
            return Result.Failure(Error.NotFound("Empleado.NotFound", $"Empleado with ID {request.Id} was not found."), 404);
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

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

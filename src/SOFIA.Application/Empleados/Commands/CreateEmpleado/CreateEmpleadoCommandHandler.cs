using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateEmpleadoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        var result = Empleado.Create(
            request.Sucursal_Base_ID,
            request.Nombres,
            request.Apellido_Paterno,
            request.Apellido_Materno,
            request.Licencia_Prof,
            request.Huella_Biometrica);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Empleados.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}

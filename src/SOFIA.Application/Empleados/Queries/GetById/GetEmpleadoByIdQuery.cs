using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Queries.GetById;

public record GetEmpleadoByIdQuery(Guid Id) : IRequest<Result<EmpleadoDto>>;

public class GetEmpleadoByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetEmpleadoByIdQuery, Result<EmpleadoDto>>
{
    public async Task<Result<EmpleadoDto>> Handle(GetEmpleadoByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Empleados
            .AsNoTracking()
            .Include(x => x.Sucursal_Base)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<EmpleadoDto>(Error.NotFound("Empleado.NotFound", $"Empleado with ID {request.Id} was not found."), 404);
        }

        var dto = new EmpleadoDto
        {
            Id = entity.Id,
            Sucursal_Base_ID = entity.Sucursal_Base_ID,
            Nombres = entity.Nombres,
            Apellido_Paterno = entity.Apellido_Paterno,
            Apellido_Materno = entity.Apellido_Materno,
            Nombre_Completo = entity.Nombre_Completo,
            Licencia_Prof = entity.Licencia_Prof,
            SucursalNombre = entity.Sucursal_Base?.Nombre
        };

        return Result.Success(dto);
    }
}

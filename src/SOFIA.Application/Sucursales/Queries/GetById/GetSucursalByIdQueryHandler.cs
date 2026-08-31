using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Queries.GetById;

public class GetSucursalByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSucursalByIdQuery, Result<SucursalDto>>
{
    public async Task<Result<SucursalDto>> Handle(GetSucursalByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Sucursales
            .AsNoTracking()
            .Include(x => x.Gerente)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<SucursalDto>(Error.NotFound("Sucursal.NotFound", $"Sucursal with ID {request.Id} was not found."), 404);
        }

        var dto = new SucursalDto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            DireccionFisica = entity.Direccion_Fisica,
            NumeroLicencia = entity.Numero_Licencia,
            GerenteId = entity.Gerente != null ? entity.Gerente_ID : null,
            GerenteNombre = entity.Gerente != null
                ? $"{entity.Gerente.Nombres} {entity.Gerente.Apellido_Paterno} {entity.Gerente.Apellido_Materno}".Trim()
                : null
        };

        return Result.Success(dto);
    }
}

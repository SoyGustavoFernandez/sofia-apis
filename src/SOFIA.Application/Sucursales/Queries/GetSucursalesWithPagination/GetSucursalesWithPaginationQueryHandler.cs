using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Queries.GetSucursalesWithPagination;

public class GetSucursalesWithPaginationQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSucursalesWithPaginationQuery, Result<PaginatedList<SucursalDto>>>
{
    public async Task<Result<PaginatedList<SucursalDto>>> Handle(GetSucursalesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = context.Sucursales
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Nombre))
        {
            query = query.Where(x => x.Nombre.Contains(request.Nombre));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroLicencia))
        {
            query = query.Where(x => x.Numero_Licencia.Contains(request.NumeroLicencia));
        }

        if (!string.IsNullOrWhiteSpace(request.DireccionFisica))
        {
            query = query.Where(x => x.Direccion_Fisica.Contains(request.DireccionFisica));
        }

        var paginatedList = await query
            .OrderBy(x => x.Nombre)
            .Select(s => new SucursalDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                DireccionFisica = s.Direccion_Fisica,
                NumeroLicencia = s.Numero_Licencia,
                GerenteId = s.Gerente != null ? s.Gerente_ID : null,
                GerenteNombre = s.Gerente != null
                    ? s.Gerente.Nombres + " " + s.Gerente.Apellido_Paterno + " " + s.Gerente.Apellido_Materno
                    : null
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}

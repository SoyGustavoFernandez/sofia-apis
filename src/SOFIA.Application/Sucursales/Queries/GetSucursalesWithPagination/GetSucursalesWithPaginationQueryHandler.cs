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

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Note: Guid doesn't support Contains. We search by Nombre or Numero_Licencia instead.
            query = query.Where(x => x.Nombre.Contains(request.SearchTerm) || x.Numero_Licencia.Contains(request.SearchTerm));
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

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Queries.GetEmpleadosWithPagination;

public class GetEmpleadosWithPaginationQueryHandler(IApplicationDbContext context) : IRequestHandler<GetEmpleadosWithPaginationQuery, Result<PaginatedList<EmpleadoDto>>>
{
    public async Task<Result<PaginatedList<EmpleadoDto>>> Handle(GetEmpleadosWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = context.Empleados
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (request.SucursalId.HasValue && request.SucursalId != Guid.Empty)
        {
            query = query.Where(x => x.Sucursal_Base_ID == request.SucursalId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Nombres.Contains(request.SearchTerm) ||
                                   x.Apellido_Paterno.Contains(request.SearchTerm) ||
                                   x.Apellido_Materno.Contains(request.SearchTerm) ||
                                   (x.Licencia_Prof != null && x.Licencia_Prof.Contains(request.SearchTerm)));
        }

        var paginatedList = await query
            .OrderBy(x => x.Apellido_Paterno)
            .ThenBy(x => x.Apellido_Materno)
            .ThenBy(x => x.Nombres)
            .Select(e => new EmpleadoDto
            {
                Id = e.Id,
                Sucursal_Base_ID = e.Sucursal_Base_ID,
                Nombres = e.Nombres,
                Apellido_Paterno = e.Apellido_Paterno,
                Apellido_Materno = e.Apellido_Materno,
                Nombre_Completo = e.Nombres + " " + e.Apellido_Paterno + " " + e.Apellido_Materno,
                Licencia_Prof = e.Licencia_Prof,
                SucursalNombre = e.Sucursal_Base != null ? e.Sucursal_Base.Nombre : null
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}

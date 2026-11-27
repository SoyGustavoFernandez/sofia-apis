using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Citas.Common;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Queries.GetCitas;

public class GetCitasQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetCitasQuery, Result<PaginatedList<CitaDto>>>
{
    public async Task<Result<PaginatedList<CitaDto>>> Handle(GetCitasQuery request, CancellationToken cancellationToken)
    {
        var sucursal = currentUser.GetSucursalId();
        if (sucursal.IsFailure)
        {
            return Result.Failure<PaginatedList<CitaDto>>(sucursal.Error);
        }

        var query = context.CitasServicio.AsNoTracking().Where(c => c.SucursalId == sucursal.Value);

        if (request.Desde is { } desde)
        {
            query = query.Where(c => c.FechaHora >= desde);
        }

        if (request.Hasta is { } hasta)
        {
            query = query.Where(c => c.FechaHora < hasta);
        }

        if (request.Estado is { } estado)
        {
            query = query.Where(c => c.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(request.PacienteNombre))
        {
            query = query.Where(c => context.Pacientes.Any(p => p.Id == c.PacienteId && p.NombreApellidos.Contains(request.PacienteNombre)));
        }

        var pagina = await PaginatedList<Domain.Entities.CitaServicio>.CreateAsync(
            query.OrderBy(c => c.FechaHora).ThenBy(c => c.Id), request.PageNumber, request.PageSize);
        var items = await CitaDtoMapper.MapAsync(context, pagina.Items, cancellationToken);

        return Result.Success(new PaginatedList<CitaDto>(items, pagina.TotalCount, pagina.PageNumber, PaginationLimits.NormalizePageSize(request.PageSize)));
    }
}

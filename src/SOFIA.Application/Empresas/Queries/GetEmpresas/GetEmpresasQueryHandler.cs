using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Queries.GetEmpresas;

public class GetEmpresasQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEmpresasQuery, Result<PaginatedList<EmpresaDto>>>
{
    public async Task<Result<PaginatedList<EmpresaDto>>> Handle(GetEmpresasQuery request, CancellationToken cancellationToken)
    {
        var query = context.Empresas
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(e => e.Sucursales)
            .Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Nombre))
        {
            query = query.Where(e => e.Nombre.Contains(request.Nombre));
        }

        if (request.Estado.HasValue)
        {
            query = query.Where(e => e.Estado == request.Estado.Value);
        }

        if (request.FechaVencimientoDesde.HasValue)
        {
            query = query.Where(e => e.FechaVencimiento >= request.FechaVencimientoDesde.Value);
        }

        if (request.FechaVencimientoHasta.HasValue)
        {
            query = query.Where(e => e.FechaVencimiento <= request.FechaVencimientoHasta.Value);
        }

        var count = await query.CountAsync(cancellationToken);

        var empresas = await query
            .OrderBy(e => e.Nombre)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = empresas.Select(e => new EmpresaDto(
            e.Id,
            e.Nombre,
            e.RUC?.Value,
            e.Estado,
            e.FechaInicioTrial,
            e.FechaVencimiento,
            e.EstaVigente,
            e.Sucursales.Count(s => !s.IsDeleted))).ToList();

        return Result.Success(new PaginatedList<EmpresaDto>(dtos, count, request.PageNumber, request.PageSize));
    }
}

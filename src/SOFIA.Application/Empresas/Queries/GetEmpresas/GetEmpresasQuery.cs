using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Queries.GetEmpresas;

public record GetEmpresasQuery(EstadoEmpresa? Estado = null) : IRequest<Result<IReadOnlyList<EmpresaDto>>>;

public class GetEmpresasQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEmpresasQuery, Result<IReadOnlyList<EmpresaDto>>>
{
    public async Task<Result<IReadOnlyList<EmpresaDto>>> Handle(GetEmpresasQuery request, CancellationToken cancellationToken)
    {
        var query = context.Empresas
            .AsNoTracking()
            .Include(e => e.Sucursales)
            .Where(e => !e.IsDeleted);

        if (request.Estado.HasValue)
        {
            query = query.Where(e => e.Estado == request.Estado.Value);
        }

        var empresas = await query.OrderBy(e => e.Nombre).ToListAsync(cancellationToken);

        IReadOnlyList<EmpresaDto> dtos = [.. empresas.Select(e => new EmpresaDto(
            e.Id,
            e.Nombre,
            e.RUC?.Value,
            e.Estado,
            e.FechaInicioTrial,
            e.FechaVencimiento,
            e.EstaVigente,
            e.Sucursales.Count(s => !s.IsDeleted)))];

        return Result.Success(dtos);
    }
}

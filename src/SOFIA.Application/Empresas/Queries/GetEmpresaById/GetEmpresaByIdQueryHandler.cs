using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empresas.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Queries.GetEmpresaById;

public class GetEmpresaByIdQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetEmpresaByIdQuery, Result<EmpresaDto>>
{
    public async Task<Result<EmpresaDto>> Handle(GetEmpresaByIdQuery request, CancellationToken cancellationToken)
    {
        var empresa = await context.EmpresasVisibles(currentUser)
            .AsNoTracking()
            .Include(e => e.Sucursales)
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        return empresa is null
            ? Result.Failure<EmpresaDto>(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404)
            : Result.Success(new EmpresaDto(
                empresa.Id,
                empresa.Nombre,
                empresa.RUC?.Value,
                empresa.Estado,
                empresa.FechaInicioTrial,
                empresa.FechaVencimiento,
                empresa.EstaVigente,
                empresa.Sucursales.Count(s => !s.IsDeleted)));
    }
}

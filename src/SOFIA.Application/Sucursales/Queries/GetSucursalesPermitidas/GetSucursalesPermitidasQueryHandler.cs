using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Queries.GetSucursalesPermitidas;

public class GetSucursalesPermitidasQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<GetSucursalesPermitidasQuery, Result<List<SucursalPermitidaDto>>>
{
    public async Task<Result<List<SucursalPermitidaDto>>> Handle(GetSucursalesPermitidasQuery request, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);

        var sucursales = await context.Sucursales
            .AsNoTracking()
            .WhereSucursalIn(s => s.Id, allowed)
            .OrderBy(s => s.Nombre)
            .Select(s => new SucursalPermitidaDto(s.Id, s.Nombre))
            .ToListAsync(cancellationToken);

        return Result.Success(sucursales);
    }
}

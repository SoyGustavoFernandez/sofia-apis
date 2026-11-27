using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Citas.Common;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Queries.GetCitaById;

public class GetCitaByIdQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetCitaByIdQuery, Result<CitaDto>>
{
    public async Task<Result<CitaDto>> Handle(GetCitaByIdQuery request, CancellationToken cancellationToken)
    {
        var sucursal = currentUser.GetSucursalId();
        if (sucursal.IsFailure)
        {
            return Result.Failure<CitaDto>(sucursal.Error);
        }

        var cita = await context.CitasServicio.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.SucursalId == sucursal.Value, cancellationToken);
        if (cita is null)
        {
            return Result.Failure<CitaDto>(Error.NotFound("CitaServicio.NotFound", "The appointment does not exist."), 404);
        }

        var dto = await CitaDtoMapper.MapAsync(context, [cita], cancellationToken);
        return Result.Success(dto[0]);
    }
}

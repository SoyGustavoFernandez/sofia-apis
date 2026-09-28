using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.SeriesFiscales.Queries.GetSerieFiscalById;

public class GetSerieFiscalByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSerieFiscalByIdQuery, Result<SerieFiscalDto>>
{
    public async Task<Result<SerieFiscalDto>> Handle(GetSerieFiscalByIdQuery request, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .AsNoTracking()
            .Include(s => s.Sucursal)
            .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

        if (serie is null)
        {
            return Result.Failure<SerieFiscalDto>(Error.NotFound("SerieFiscal.NotFound", "The specified fiscal series does not exist."), 404);
        }

        var tieneComprobantes = await context.SUNATComprobantesEmitidos
            .AnyAsync(c => c.SerieId == serie.Id && !c.IsDeleted, cancellationToken);

        return Result.Success(new SerieFiscalDto(
            serie.Id,
            serie.SucursalId,
            serie.Sucursal?.Nombre ?? string.Empty,
            serie.TipoComprobante,
            serie.PrefijoSerie,
            serie.CorrelativoActual,
            serie.EstadoSerie,
            tieneComprobantes,
            serie.CreatedAt));
    }
}

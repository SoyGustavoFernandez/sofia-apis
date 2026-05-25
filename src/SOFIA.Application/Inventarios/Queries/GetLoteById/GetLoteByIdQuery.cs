using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLoteById;

public record GetLoteByIdQuery(Guid Id) : IRequest<Result<LoteInventarioDto>>;

public class GetLoteByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLoteByIdQuery, Result<LoteInventarioDto>>
{
    public async Task<Result<LoteInventarioDto>> Handle(GetLoteByIdQuery request, CancellationToken cancellationToken)
    {
        var lote = await context.LotesInventario
            .AsNoTracking()
            .Include(x => x.Producto)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        return lote is null
            ? Result.Failure<LoteInventarioDto>(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."))
            : Result.Success(new LoteInventarioDto(
                lote.Id,
                lote.ProductoId,
                lote.Producto != null ? lote.Producto.NombreComercial : "Unknown",
                lote.NumeroLoteMfr,
                lote.FechaFabricacion,
                lote.FechaCaducidad));
    }
}

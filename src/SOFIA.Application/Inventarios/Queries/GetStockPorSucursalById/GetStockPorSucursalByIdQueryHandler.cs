using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetStockPorSucursalById;

public class GetStockPorSucursalByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockPorSucursalByIdQuery, Result<StockPorSucursalDto>>
{
    public async Task<Result<StockPorSucursalDto>> Handle(GetStockPorSucursalByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await context.LotesEnSucursal
            .AsNoTracking()
            .Include(x => x.Sucursal)
            .Include(x => x.Lote)
            .ThenInclude(l => l!.Producto)
            .Where(x => x.Id == request.Id && x.Sucursal != null && x.Lote != null && x.Lote.Producto != null)
            .Select(x => new StockPorSucursalDto(
                x.Id,
                x.SucursalId,
                x.Sucursal!.Nombre,
                x.LoteId,
                x.Lote!.NumeroLoteMfr,
                x.Lote.ProductoId,
                x.Lote.Producto!.NombreComercial,
                x.Lote.FechaCaducidad,
                x.CantidadFisica))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Failure<StockPorSucursalDto>(Error.NotFound("InventarioSucursal.NotFound", "The specified stock record does not exist."))
            : Result.Success(dto);
    }
}

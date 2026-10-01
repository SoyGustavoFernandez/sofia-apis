using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetLotesDisponiblesVenta;

public class GetLotesDisponiblesVentaQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLotesDisponiblesVentaQuery, Result<List<LoteDisponibleVentaDto>>>
{
    private const int MaxLotes = 50;

    public async Task<Result<List<LoteDisponibleVentaDto>>> Handle(GetLotesDisponiblesVentaQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<List<LoteDisponibleVentaDto>>(sucursalResult.Error);
        }

        if (!request.ProductoId.HasValue && string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            return Result.Failure<List<LoteDisponibleVentaDto>>(Error.Validation("Venta.LotesDisponibles.ProductoRequerido", "A product id or name is required."));
        }

        var query = context.LotesEnSucursal
            .AsNoTracking()
            .Where(x => x.Lote!.Producto != null)
            .WhereVendibleEnSucursal(context.DigemidInventarioCuarentena, sucursalResult.Value, DateTimeOffset.UtcNow);

        if (request.ProductoId.HasValue)
        {
            query = query.Where(x => x.Lote!.ProductoId == request.ProductoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Lote!.Producto!.NombreComercial.ToLower().Contains(term));
        }

        // First to expire first, so the cashier is offered the lot that should leave the shelf next.
        var lotes = await query
            .OrderBy(x => x.Lote!.FechaCaducidad)
            .ThenBy(x => x.Lote!.NumeroLoteMfr)
            .Take(MaxLotes)
            .Select(x => new LoteDisponibleVentaDto(
                x.Id,
                x.LoteId,
                x.Lote!.NumeroLoteMfr,
                x.Lote.ProductoId,
                x.Lote.Producto!.NombreComercial,
                x.Lote.FechaCaducidad,
                x.CantidadFisica,
                x.Lote.Producto!.PrecioVentaBase))
            .ToListAsync(cancellationToken);

        return Result.Success(lotes);
    }
}

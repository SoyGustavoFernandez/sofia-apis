using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;

public record GetStockByMedicamentoQuery(Guid MedicamentoId) : IRequest<Result<StockMedicamentoDto>>;

public class GetStockByMedicamentoQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockByMedicamentoQuery, Result<StockMedicamentoDto>>
{
    public async Task<Result<StockMedicamentoDto>> Handle(GetStockByMedicamentoQuery request, CancellationToken cancellationToken)
    {
        var medicamento = await context.Medicamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.MedicamentoId, cancellationToken);

        if (medicamento == null)
        {
            return Result.Failure<StockMedicamentoDto>(Error.NotFound("Medicamento.NotFound", "The specified medication does not exist."));
        }

        var inventoryEntries = await context.LotesEnSucursal
            .AsNoTracking()
            .Include(x => x.Lote)
            .Include(x => x.Sucursal)
            .Where(x => x.Lote != null && x.Lote.ProductoId == request.MedicamentoId)
            .ToListAsync(cancellationToken);

        var totalStock = inventoryEntries.Sum(x => x.CantidadFisica);

        var desgloseSucursales = inventoryEntries
            .GroupBy(x => new { x.SucursalId, Nombre = x.Sucursal?.Nombre ?? "Unknown" })
            .Select(g => new StockSucursalDto(g.Key.SucursalId, g.Key.Nombre, g.Sum(x => x.CantidadFisica)))
            .ToList();

        var desgloseLotes = inventoryEntries
            .GroupBy(x => new { x.LoteId, Numero = x.Lote?.NumeroLoteMfr ?? "Unknown", Caducidad = x.Lote?.FechaCaducidad ?? DateTimeOffset.MinValue })
            .Select(g => new StockLoteDto(g.Key.LoteId, g.Key.Numero, g.Key.Caducidad, g.Sum(x => x.CantidadFisica)))
            .OrderBy(x => x.FechaCaducidad)
            .ToList();

        return Result.Success(new StockMedicamentoDto(
            medicamento.Id,
            medicamento.NombreComercial,
            totalStock,
            desgloseSucursales,
            desgloseLotes));
    }
}

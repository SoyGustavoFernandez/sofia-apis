using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Queries.GetStockByMedicamento;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentoById;

public record GetMedicamentoByIdQuery(Guid Id) : IRequest<Result<MedicamentoDto>>;

public class GetMedicamentoByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetMedicamentoByIdQuery, Result<MedicamentoDto>>
{
    public async Task<Result<MedicamentoDto>> Handle(GetMedicamentoByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Medicamentos
            .AsNoTracking()
            .Include(x => x.Laboratorio)
            .Include(x => x.UnidadBase)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<MedicamentoDto>(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.Id} was not found."), 404);
        }

        // Fetch stock info
        var inventoryEntries = await context.LotesEnSucursal
            .AsNoTracking()
            .Include(x => x.Lote)
            .Include(x => x.Sucursal)
            .Where(x => x.Lote != null && x.Lote.ProductoId == entity.Id)
            .ToListAsync(cancellationToken);

        var totalStock = inventoryEntries.Sum(x => x.CantidadFisica);

        var stockPorSucursal = inventoryEntries
            .GroupBy(x => new { x.SucursalId, Nombre = x.Sucursal?.Nombre ?? "Unknown" })
            .Select(g => new StockSucursalDto(g.Key.SucursalId, g.Key.Nombre, g.Sum(x => x.CantidadFisica)))
            .ToList();

        var dto = new MedicamentoDto(
            entity.Id,
            entity.CodigoNacional,
            entity.NombreComercial,
            entity.LaboratorioId,
            entity.Laboratorio?.NombreCompania ?? "Unknown",
            entity.UnidadBaseId,
            entity.UnidadBase?.Descripcion ?? "Unknown",
            entity.CondicionVenta,
            totalStock,
            stockPorSucursal);

        return Result.Success(dto);
    }
}

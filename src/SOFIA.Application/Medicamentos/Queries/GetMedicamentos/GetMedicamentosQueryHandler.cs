using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentos;

public class GetMedicamentosQueryHandler(IApplicationDbContext context) : IRequestHandler<GetMedicamentosQuery, Result<PaginatedList<MedicamentoDto>>>
{
    public async Task<Result<PaginatedList<MedicamentoDto>>> Handle(GetMedicamentosQuery request, CancellationToken cancellationToken)
    {
        var query = context.Medicamentos
            .AsNoTracking()
            .Include(x => x.Laboratorio)
            .Include(x => x.UnidadBase)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.CodigoNacional.ToLower().Contains(searchTerm) ||
                                     x.NombreComercial.ToLower().Contains(searchTerm) ||
                                     (x.Laboratorio != null && x.Laboratorio.NombreCompania.ToLower().Contains(searchTerm)));
        }

        query = query.OrderBy(x => x.NombreComercial);

        var paginatedList = await PaginatedList<MedicamentoDto>.CreateAsync(
            query.Select(x => new MedicamentoDto(
                x.Id,
                x.CodigoNacional,
                x.NombreComercial,
                x.LaboratorioId,
                x.Laboratorio != null ? x.Laboratorio.NombreCompania : "Unknown",
                x.UnidadBaseId,
                x.UnidadBase != null ? x.UnidadBase.Descripcion : "Unknown",
                x.CondicionVenta,
                context.LotesEnSucursal
                    .Where(ls => ls.Lote != null && ls.Lote.ProductoId == x.Id)
                    .Sum(ls => ls.CantidadFisica))),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}

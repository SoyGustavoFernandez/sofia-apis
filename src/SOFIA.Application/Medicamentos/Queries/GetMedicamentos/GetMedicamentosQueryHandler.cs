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
        // Laboratorio and UnidadBase are required; a medicamento whose principal was soft-deleted is invalid data and is excluded.
        var query = context.Medicamentos
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Laboratorio != null && x.UnidadBase != null);

        if (!string.IsNullOrWhiteSpace(request.CodigoNacional))
        {
            var term = request.CodigoNacional.ToLower();
            query = query.Where(x => x.CodigoNacional.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.NombreComercial))
        {
            var term = request.NombreComercial.ToLower();
            query = query.Where(x => x.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.LaboratorioNombre))
        {
            var term = request.LaboratorioNombre.ToLower();
            query = query.Where(x => x.Laboratorio!.NombreCompania.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.UnidadBaseNombre))
        {
            var term = request.UnidadBaseNombre.ToLower();
            query = query.Where(x => x.UnidadBase!.Descripcion.ToLower().Contains(term));
        }

        if (request.CondicionVenta.HasValue)
        {
            query = query.Where(x => x.CondicionVenta == request.CondicionVenta.Value);
        }

        var paginatedList = await PaginatedList<MedicamentoDto>.CreateAsync(
            query.OrderBy(x => x.NombreComercial)
                .Select(x => new MedicamentoDto(
                    x.Id,
                    x.CodigoNacional,
                    x.NombreComercial,
                    x.LaboratorioId,
                    x.Laboratorio!.NombreCompania,
                    x.UnidadBaseId,
                    x.UnidadBase!.Descripcion,
                    x.CondicionVenta)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}

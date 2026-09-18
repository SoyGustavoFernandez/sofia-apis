using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentos;

public record GetMedicamentosQuery : IRequest<Result<PaginatedList<MedicamentoDto>>>
{
    public string? CodigoNacional { get; init; }
    public string? NombreComercial { get; init; }
    public string? LaboratorioNombre { get; init; }
    public string? UnidadBaseNombre { get; init; }
    public CondicionVenta? CondicionVenta { get; init; }

    // Opt-in: keeps the stock join off the Excel export, which reuses this query at PageSize = int.MaxValue.
    public bool IncluirStock { get; init; }

    // POS default view only: ranks by units sold in the caller's own sucursal over the last 30 days instead of by name.
    public bool OrdenarPorMasVendidos { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

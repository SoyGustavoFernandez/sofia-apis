using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoras;

public record AseguradoraDto(Guid Id, string NombreComercial, string CodigoIdentificadorNacional);

public record GetAseguradorasQuery : IRequest<Result<PaginatedList<AseguradoraDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

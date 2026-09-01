using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogo;

public record DigemidProductoDto(Guid Id, string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? RegistroSanitario, string? Titular, string Estado);

public record GetDigemidCatalogoQuery : IRequest<Result<PaginatedList<DigemidProductoDto>>>
{
    public string? CodProd { get; init; }
    public string? NomProd { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

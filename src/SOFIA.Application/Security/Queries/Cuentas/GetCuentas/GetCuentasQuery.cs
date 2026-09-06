using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Cuentas.GetCuentas;

public record GetCuentasQuery : IRequest<Result<PaginatedList<CuentaDto>>>
{
    public string? NombreUsuario { get; init; }
    public string? NombreEmpleado { get; init; }
    public bool? CuentaActiva { get; init; }
    public bool? Bloqueado { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

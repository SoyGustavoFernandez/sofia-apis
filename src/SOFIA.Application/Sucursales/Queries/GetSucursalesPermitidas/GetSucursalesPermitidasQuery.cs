using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Queries.GetSucursalesPermitidas;

public record GetSucursalesPermitidasQuery : IRequest<Result<List<SucursalPermitidaDto>>>;

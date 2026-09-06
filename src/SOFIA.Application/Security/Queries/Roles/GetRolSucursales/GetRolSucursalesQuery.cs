using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRolSucursales;

public record GetRolSucursalesQuery(Guid RolId) : IRequest<Result<List<SucursalRolItem>>>;

public record SucursalRolItem(Guid Id, string Nombre, bool Asignada);

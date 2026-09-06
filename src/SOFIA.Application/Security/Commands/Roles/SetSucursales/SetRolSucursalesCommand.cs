using MediatR;
using SOFIA.Application.Common.Behaviors;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.SetSucursales;

public record SetRolSucursalesCommand(Guid RolId, List<Guid> SucursalIds) : IRequest<Result>, IBaseCommand;

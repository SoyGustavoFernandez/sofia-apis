using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetPermissions;

public record GetPermissionsByRolQuery(Guid RolId) : IRequest<Result<IReadOnlyList<PermisoResponse>>>;

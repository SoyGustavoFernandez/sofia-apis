using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRoles;

public record GetRolesQuery() : IRequest<Result<IReadOnlyList<RolResponse>>>;

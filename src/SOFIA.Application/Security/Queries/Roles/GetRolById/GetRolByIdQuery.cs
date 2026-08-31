using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRolById;

public record GetRolByIdQuery(Guid Id) : IRequest<Result<RolResponse>>;

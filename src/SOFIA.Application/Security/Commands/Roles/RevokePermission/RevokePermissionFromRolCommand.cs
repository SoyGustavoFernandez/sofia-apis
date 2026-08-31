using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RevokePermission;

public record RevokePermissionFromRolCommand(Guid PermisoId) : ICommand;

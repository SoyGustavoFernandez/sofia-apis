using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignPermission;

public record AssignPermissionToRolCommand(Guid RolId, string ModuloSistema, string Accion) : ICommand<Guid>;

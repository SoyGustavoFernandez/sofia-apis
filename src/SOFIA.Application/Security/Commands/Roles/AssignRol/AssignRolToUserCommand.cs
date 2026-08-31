using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.AssignRol;

public record AssignRolToUserCommand(Guid CuentaId, Guid RolId) : ICommand;

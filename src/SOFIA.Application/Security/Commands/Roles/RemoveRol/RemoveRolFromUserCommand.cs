using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RemoveRol;

public record RemoveRolFromUserCommand(Guid CuentaId, Guid RolId) : ICommand;

using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.CreateRol;

public record CreateRolCommand(string NombreRol, string? Descripcion, int NivelJerarquia) : ICommand<Guid>;

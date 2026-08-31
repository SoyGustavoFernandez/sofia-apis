using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Register;

public record RegisterAccountCommand(
    Guid EmpleadoId,
    string NombreUsuario,
    string Password) : ICommand<Guid>;

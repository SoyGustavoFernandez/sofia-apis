using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empleados.Commands.DeleteEmpleado;

public record DeleteEmpleadoCommand(Guid Id) : ICommand;

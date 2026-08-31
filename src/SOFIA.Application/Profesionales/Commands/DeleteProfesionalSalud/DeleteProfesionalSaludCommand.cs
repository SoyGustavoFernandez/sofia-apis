using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;

public record DeleteProfesionalSaludCommand(Guid Id) : ICommand;

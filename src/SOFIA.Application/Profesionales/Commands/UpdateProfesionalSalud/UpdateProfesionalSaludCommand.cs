using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Commands.UpdateProfesionalSalud;

public record UpdateProfesionalSaludCommand(Guid Id, string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica) : ICommand;

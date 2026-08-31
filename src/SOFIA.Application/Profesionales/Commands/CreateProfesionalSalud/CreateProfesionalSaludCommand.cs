using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;

public record CreateProfesionalSaludCommand(string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica) : ICommand<Guid>;

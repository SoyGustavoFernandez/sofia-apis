using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;

public record CreateLaboratorioCommand(string NombreCompania, string? CodigoIdentificador) : ICommand<Guid>;

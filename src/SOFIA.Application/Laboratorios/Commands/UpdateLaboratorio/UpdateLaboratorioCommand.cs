using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;

public record UpdateLaboratorioCommand(Guid Id, string NombreCompania, string? CodigoIdentificador) : ICommand;

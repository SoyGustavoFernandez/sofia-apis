using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;

public record DeleteLaboratorioCommand(Guid Id) : ICommand;

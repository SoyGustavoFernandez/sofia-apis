using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;

public record DeleteMedicamentoCommand(Guid Id) : ICommand;

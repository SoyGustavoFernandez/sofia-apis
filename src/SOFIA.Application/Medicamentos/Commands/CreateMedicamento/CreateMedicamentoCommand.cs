using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CreateMedicamento;

public record CreateMedicamentoCommand(
    string CodigoNacional,
    string NombreComercial,
    Guid LaboratorioId,
    Guid UnidadBaseId,
    Domain.Enums.CondicionVenta CondicionVenta) : ICommand<Guid>;

// Validator = pipeline fast-fail; entity method = domain invariant. Both layers are intentional.

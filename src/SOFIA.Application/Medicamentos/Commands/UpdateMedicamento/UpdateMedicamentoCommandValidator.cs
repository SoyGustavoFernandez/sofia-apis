using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;

public class UpdateMedicamentoCommandValidator : AbstractValidator<UpdateMedicamentoCommand>
{
    public UpdateMedicamentoCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.CodigoNacional)
            .NotEmpty().WithMessage("CÃ³digo Nacional is required.")
            .MaximumLength(Medicamento.CodigoNacionalMaxLength).WithMessage($"CÃ³digo Nacional must not exceed {Medicamento.CodigoNacionalMaxLength} characters.");

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Nombre Comercial is required.")
            .MaximumLength(Medicamento.NombreComercialMaxLength).WithMessage($"Nombre Comercial must not exceed {Medicamento.NombreComercialMaxLength} characters.");

        _ = RuleFor(v => v.LaboratorioId)
            .NotEmpty().WithMessage("Laboratorio ID is required.");

        _ = RuleFor(v => v.UnidadBaseId)
            .NotEmpty().WithMessage("Unidad Base ID is required.");

        _ = RuleFor(v => v.CondicionVenta)
            .IsInEnum().WithMessage("Invalid CondiciÃ³n de Venta.");
    }
}

using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;

public record UpdateMedicamentoCommand(
    Guid Id,
    string CodigoNacional,
    string NombreComercial,
    Guid LaboratorioId,
    Guid UnidadBaseId,
    Domain.Enums.CondicionVenta CondicionVenta) : IRequest<Result>;

// Validator = pipeline fast-fail; entity method = domain invariant. Both layers are intentional.
public class UpdateMedicamentoCommandValidator : AbstractValidator<UpdateMedicamentoCommand>
{
    public UpdateMedicamentoCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.CodigoNacional)
            .NotEmpty().WithMessage("Código Nacional is required.")
            .MaximumLength(Medicamento.CodigoNacionalMaxLength).WithMessage($"Código Nacional must not exceed {Medicamento.CodigoNacionalMaxLength} characters.");

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Nombre Comercial is required.")
            .MaximumLength(Medicamento.NombreComercialMaxLength).WithMessage($"Nombre Comercial must not exceed {Medicamento.NombreComercialMaxLength} characters.");

        _ = RuleFor(v => v.LaboratorioId)
            .NotEmpty().WithMessage("Laboratorio ID is required.");

        _ = RuleFor(v => v.UnidadBaseId)
            .NotEmpty().WithMessage("Unidad Base ID is required.");

        _ = RuleFor(v => v.CondicionVenta)
            .IsInEnum().WithMessage("Invalid Condición de Venta.");
    }
}

public class UpdateMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateMedicamentoCommand, Result>
{
    public async Task<Result> Handle(UpdateMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Medicamentos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(
            request.CodigoNacional,
            request.NombreComercial,
            request.LaboratorioId,
            request.UnidadBaseId,
            request.CondicionVenta);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

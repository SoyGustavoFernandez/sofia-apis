using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Commands.UpdateEmpresa;

public record UpdateEmpresaCommand : ICommand
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? RUC { get; init; }
}

public class UpdateEmpresaCommandValidator : AbstractValidator<UpdateEmpresaCommand>
{
    public UpdateEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        _ = When(x => x.RUC is not null, () =>
            RuleFor(x => x.RUC)
                .Length(11).WithMessage("RUC must be exactly 11 digits.")
                .Matches("^[0-9]{11}$").WithMessage("RUC must contain only digits."));
    }
}

public class UpdateEmpresaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateEmpresaCommand, Result>
{
    public async Task<Result> Handle(UpdateEmpresaCommand request, CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        if (empresa is null)
        {
            return Result.Failure(Error.NotFound("Empresa.NotFound", "La empresa no existe."), 404);
        }

        if (request.RUC is not null)
        {
            var rucTomado = await context.Empresas
                .AnyAsync(e => e.RUC == request.RUC && e.Id != request.Id && !e.IsDeleted, cancellationToken);
            if (rucTomado)
            {
                return Result.Failure(Error.Conflict("Empresa.RUC.Duplicado", "Ya existe otra empresa con este RUC."), 409);
            }
        }

        var result = empresa.Update(request.Nombre, request.RUC);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

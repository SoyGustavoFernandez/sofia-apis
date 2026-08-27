using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.UpdateSucursal;

public record UpdateSucursalCommand : ICommand
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string DireccionFisica { get; init; } = string.Empty;
    public string NumeroLicencia { get; init; } = string.Empty;
    public Guid? GerenteId { get; init; }
}

public class UpdateSucursalCommandValidator : AbstractValidator<UpdateSucursalCommand>
{
    public UpdateSucursalCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.Nombre)
            .NotEmpty().WithMessage("Nombre is required.")
            .MaximumLength(100).WithMessage("Nombre must not exceed 100 characters.");

        _ = RuleFor(v => v.DireccionFisica)
            .NotEmpty().WithMessage("Direccion Fisica is required.")
            .MaximumLength(255).WithMessage("Direccion Fisica must not exceed 255 characters.");

        _ = RuleFor(v => v.NumeroLicencia)
            .NotEmpty().WithMessage("Numero Licencia is required.")
            .MaximumLength(50).WithMessage("Numero Licencia must not exceed 50 characters.");
    }
}

public class UpdateSucursalCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateSucursalCommand, Result>
{
    public async Task<Result> Handle(UpdateSucursalCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Sucursales
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", $"Sucursal with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(
            request.Nombre,
            request.DireccionFisica,
            request.NumeroLicencia,
            request.GerenteId);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

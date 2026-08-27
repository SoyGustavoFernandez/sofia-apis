using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CreateSucursal;

public record CreateSucursalCommand : ICommand<Guid>
{
    public string Nombre { get; init; } = string.Empty;
    public string DireccionFisica { get; init; } = string.Empty;
    public string NumeroLicencia { get; init; } = string.Empty;
    public Guid? GerenteId { get; init; }
}

public class CreateSucursalCommandValidator : AbstractValidator<CreateSucursalCommand>
{
    public CreateSucursalCommandValidator()
    {
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

public class CreateSucursalCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateSucursalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSucursalCommand request, CancellationToken cancellationToken)
    {
        var result = Sucursal.Create(
            request.Nombre,
            request.DireccionFisica,
            request.NumeroLicencia,
            request.GerenteId);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Sucursales.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

public record AnonimizarDatosCommand(string Tabla, Guid RegistroId) : IRequest<Result<Guid>>;

public class AnonimizarDatosCommandValidator : AbstractValidator<AnonimizarDatosCommand>
{
    public AnonimizarDatosCommandValidator()
    {
        _ = RuleFor(v => v.Tabla).NotEmpty();
        _ = RuleFor(v => v.RegistroId).NotEmpty();
    }
}

public class AnonimizarDatosCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AnonimizarDatosCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AnonimizarDatosCommand request, CancellationToken cancellationToken) =>
        // Simulate Presidio anonymization
        Result.Success(Guid.NewGuid());
}

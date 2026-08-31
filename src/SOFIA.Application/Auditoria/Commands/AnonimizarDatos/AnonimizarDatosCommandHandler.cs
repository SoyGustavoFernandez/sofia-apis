using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

public class AnonimizarDatosCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AnonimizarDatosCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AnonimizarDatosCommand request, CancellationToken cancellationToken) =>
        // Simulate Presidio anonymization
        Result.Success(Guid.NewGuid());
}

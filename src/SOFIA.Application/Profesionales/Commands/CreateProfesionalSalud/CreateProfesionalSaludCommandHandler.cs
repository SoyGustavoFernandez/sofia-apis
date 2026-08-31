using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;

public class CreateProfesionalSaludCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateProfesionalSaludCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProfesionalSaludCommand request, CancellationToken cancellationToken)
    {
        var result = ProfesionalSalud.Create(request.NumeroRegistro, request.NombrePrescriptor, request.DireccionClinica);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.ProfesionalesSalud.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}

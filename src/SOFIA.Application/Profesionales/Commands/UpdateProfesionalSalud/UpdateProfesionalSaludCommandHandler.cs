using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Commands.UpdateProfesionalSalud;

public class UpdateProfesionalSaludCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateProfesionalSaludCommand, Result>
{
    public async Task<Result> Handle(UpdateProfesionalSaludCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ProfesionalesSalud
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("ProfesionalSalud.NotFound", $"ProfesionalSalud with ID {request.Id} was not found."), 404);
        }

        var duplicado = await context.ProfesionalesSalud
            .AnyAsync(p => p.Id != entity.Id && p.NumeroRegistro == request.NumeroRegistro && !p.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("ProfesionalSalud.NumeroRegistro.Duplicado", "Another health professional already uses this registration number."), 409);
        }

        var result = entity.Update(request.NumeroRegistro, request.NombrePrescriptor, request.DireccionClinica);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

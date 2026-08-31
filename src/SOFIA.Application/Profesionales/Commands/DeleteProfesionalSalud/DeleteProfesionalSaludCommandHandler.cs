using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;

public class DeleteProfesionalSaludCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteProfesionalSaludCommand, Result>
{
    public async Task<Result> Handle(DeleteProfesionalSaludCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ProfesionalesSalud
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("ProfesionalSalud.NotFound", $"ProfesionalSalud with ID {request.Id} was not found."), 404);
        }

        _ = context.ProfesionalesSalud.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

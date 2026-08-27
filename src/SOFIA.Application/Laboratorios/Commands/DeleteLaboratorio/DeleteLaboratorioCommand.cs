using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;

public record DeleteLaboratorioCommand(Guid Id) : ICommand;

public class DeleteLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteLaboratorioCommand, Result>
{
    public async Task<Result> Handle(DeleteLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Laboratorios
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Laboratorio.NotFound", $"Laboratorio with ID {request.Id} was not found."), 404);
        }

        _ = context.Laboratorios.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

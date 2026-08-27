using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Delete;

public record DeleteFormulacionClinicaCommand(Guid Id) : ICommand;

public class DeleteFormulacionClinicaCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteFormulacionClinicaCommand, Result>
{
    public async Task<Result> Handle(DeleteFormulacionClinicaCommand request, CancellationToken cancellationToken)
    {
        var formulacion = await context.FormulacionesClinicas
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (formulacion is null)
        {
            return Result.Failure(Error.NotFound("Formulacion.NotFound", $"Formulación with ID {request.Id} not found."));
        }

        _ = context.FormulacionesClinicas.Remove(formulacion);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

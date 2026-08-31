using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;

public class UpdateLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateLaboratorioCommand, Result>
{
    public async Task<Result> Handle(UpdateLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Laboratorios
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Laboratorio.NotFound", $"Laboratorio with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.NombreCompania, request.CodigoIdentificador);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

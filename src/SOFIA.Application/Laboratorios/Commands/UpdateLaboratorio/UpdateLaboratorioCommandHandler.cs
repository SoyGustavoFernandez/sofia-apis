using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Common;
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

        var codigo = string.IsNullOrWhiteSpace(request.CodigoIdentificador) ? null : request.CodigoIdentificador;
        var duplicado = await LaboratorioDuplicateChecker.FindAsync(context, entity.Id, request.NombreCompania, codigo, cancellationToken);
        if (duplicado is not null)
        {
            return Result.Failure(duplicado, 409);
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

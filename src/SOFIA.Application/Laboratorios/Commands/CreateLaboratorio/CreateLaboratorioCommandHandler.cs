using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;

public class CreateLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateLaboratorioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var result = Laboratorio.Create(request.NombreCompania, request.CodigoIdentificador);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Laboratorios.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}

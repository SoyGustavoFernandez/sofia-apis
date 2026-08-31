using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;

public class CreateUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateUnidadMedidaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var result = UnidadMedida.Create(request.Codigo, request.Descripcion);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.UnidadesMedida.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}

using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;

public record DeleteMedicamentoCommand(Guid Id) : IRequest<Result>;

public class DeleteMedicamentoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteMedicamentoCommand, Result>
{
    public async Task<Result> Handle(DeleteMedicamentoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Medicamentos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.Id} was not found."), 404);
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}

using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Seguros.Commands.DeleteAseguradora;

public record DeleteAseguradoraCommand(Guid Id) : ICommand<Guid>;

public class DeleteAseguradoraCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteAseguradoraCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteAseguradoraCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Aseguradoras
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<Guid>(Error.NotFound("NotFound", "No se encontró el registro."));
        }

        _ = context.Aseguradoras.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
}
